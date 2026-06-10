using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Application.Abstractions.Storage;

namespace Infrastructure.Storage;

internal sealed class GarageAttachmentStorage : IAttachmentStorage
{
    private readonly IHttpClientFactory _http;
    private readonly string _accessKey;
    private readonly string _secretKey;
    private readonly string _endpoint;
    private readonly string _bucket;
    private readonly string _region;
    private readonly string _publicUrl;

    internal GarageAttachmentStorage(
        IHttpClientFactory http,
        string accessKey,
        string secretKey,
        string endpoint,
        string bucket,
        string region,
        string publicUrl)
    {
        _http = http;
        _accessKey = accessKey;
        _secretKey = secretKey;
        _endpoint = endpoint.TrimEnd('/');
        _bucket = bucket;
        _region = region;
        _publicUrl = publicUrl.TrimEnd('/');
    }

    public async Task<string> UploadAsync(string key, Stream content, string contentType)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer);
        var body = buffer.ToArray();

        using var response = await SignedRequestAsync(HttpMethod.Put, key, body, contentType);
        response.EnsureSuccessStatusCode();
        return GetPermanentUrl(key);
    }

    public async Task<Stream> DownloadAsync(string key)
    {
        using var response = await SignedRequestAsync(HttpMethod.Get, key, body: null, contentType: null);
        response.EnsureSuccessStatusCode();
        var ms = new MemoryStream();
        await response.Content.CopyToAsync(ms);
        ms.Position = 0;
        return ms;
    }

    public async Task DeleteAsync(string key)
    {
        using var response = await SignedRequestAsync(HttpMethod.Delete, key, body: null, contentType: null);
        response.EnsureSuccessStatusCode();
    }

    // Génère une URL pré-signée (AWS Signature V4) valable X minutes
    // Le frontend peut utiliser cette URL pour accéder à l'image sans credentials
    public string GetPresignedUrl(string key, int expirationMinutes = 30)
    {
        var encodedKey = string.Join("/", key.Split('/').Select(Uri.EscapeDataString));
        var uri = new Uri($"{_endpoint}/{_bucket}/{encodedKey}");

        var now = DateTime.UtcNow;
        var amzDate = now.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);
        var dateStamp = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var expiresIn = expirationMinutes * 60; // convertir en secondes
        
        var host = uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";

        // Credential scope pour la signature
        var credentialScope = $"{dateStamp}/{_region}/s3/aws4_request";
        var credential = Uri.EscapeDataString($"{_accessKey}/{credentialScope}");

        // Query string avec les paramètres de signature
        var canonicalQueryString = 
            $"X-Amz-Algorithm=AWS4-HMAC-SHA256&" +
            $"X-Amz-Credential={credential}&" +
            $"X-Amz-Date={amzDate}&" +
            $"X-Amz-Expires={expiresIn}&" +
            $"X-Amz-SignedHeaders=host";

        // Requête canonique pour GET (sans corps)
        var canonicalRequest = string.Join("\n",
            "GET",
            $"/{_bucket}/{encodedKey}",
            canonicalQueryString,
            $"host:{host}\n",
            "host",
            Hex(SHA256.HashData([])));

        // String to sign
        var stringToSign = string.Join("\n",
            "AWS4-HMAC-SHA256",
            amzDate,
            credentialScope,
            Hex(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest))));

        // Dériver la clé de signature et calculer la signature
        var signingKey = DeriveSigningKey(_secretKey, dateStamp, _region, "s3");
        var signature = Hex(HmacSha256(signingKey, Encoding.UTF8.GetBytes(stringToSign)));

        // Retourner l'URL complète avec tous les paramètres de signature
        return $"{uri}?{canonicalQueryString}&X-Amz-Signature={signature}";
    }

    // Retourne une URL pré-signée pour l'accès public
    // ou un proxy si _publicUrl est configurée
    public string GetPermanentUrl(string key) =>
        string.IsNullOrEmpty(_publicUrl)
            ? GetPresignedUrl(key, expirationMinutes: 30)  // URL pré-signée valable 30 minutes
            : $"{_publicUrl}/files/{key}";                 // Ou un proxy API

    // Minimal AWS Signature Version 4 pour les requêtes S3 path-style.
    // On signe manuellement pour éviter les limitations de AWSSDK (streaming payload)
    // qui ne fonctionne pas bien avec Garage.
    private async Task<HttpResponseMessage> SignedRequestAsync(
        HttpMethod method,
        string key,
        byte[]? body,
        string? contentType)
    {
        var encodedKey = string.Join("/", key.Split('/').Select(Uri.EscapeDataString));
        var uri = new Uri($"{_endpoint}/{_bucket}/{encodedKey}");

        var now = DateTime.UtcNow;
        var amzDate = now.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);
        var dateStamp = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var host = uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";

        var bodyBytes = body ?? [];
        var bodyHash = Hex(SHA256.HashData(bodyBytes));

        // Les headers doivent être triés alphabétiquement
        var hdrs = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["host"] = host,
            ["x-amz-content-sha256"] = bodyHash,
            ["x-amz-date"] = amzDate,
        };
        if (!string.IsNullOrEmpty(contentType))
        {
            hdrs["content-type"] = contentType;
        }

        var canonicalHeaders = string.Concat(hdrs.Select(h => $"{h.Key}:{h.Value}\n"));
        var signedHeaders = string.Join(";", hdrs.Keys);

        var canonicalRequest = string.Join("\n",
            method.Method,
            $"/{_bucket}/{encodedKey}",
            "",
            canonicalHeaders,
            signedHeaders,
            bodyHash);

        var credentialScope = $"{dateStamp}/{_region}/s3/aws4_request";
        var stringToSign = string.Join("\n",
            "AWS4-HMAC-SHA256",
            amzDate,
            credentialScope,
            Hex(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest))));

        var signingKey = DeriveSigningKey(_secretKey, dateStamp, _region, "s3");
        var signature = Hex(HmacSha256(signingKey, Encoding.UTF8.GetBytes(stringToSign)));

        var authorization =
            $"AWS4-HMAC-SHA256 Credential={_accessKey}/{credentialScope}, " +
            $"SignedHeaders={signedHeaders}, Signature={signature}";

        using var request = new HttpRequestMessage(method, uri);
        request.Headers.TryAddWithoutValidation("Authorization", authorization);
        request.Headers.TryAddWithoutValidation("x-amz-date", amzDate);
        request.Headers.TryAddWithoutValidation("x-amz-content-sha256", bodyHash);

        if (bodyBytes.Length > 0)
        {
            request.Content = new ByteArrayContent(bodyBytes);
            if (!string.IsNullOrEmpty(contentType))
            {
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            }
        }

        var client = _http.CreateClient("garage");
        return await client.SendAsync(request);
    }

    private static byte[] DeriveSigningKey(string secret, string date, string region, string service)
    {
        var kDate    = HmacSha256(Encoding.UTF8.GetBytes("AWS4" + secret), Encoding.UTF8.GetBytes(date));
        var kRegion  = HmacSha256(kDate, Encoding.UTF8.GetBytes(region));
        var kService = HmacSha256(kRegion, Encoding.UTF8.GetBytes(service));
        return HmacSha256(kService, Encoding.UTF8.GetBytes("aws4_request"));
    }

    private static byte[] HmacSha256(byte[] key, byte[] data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(data);
    }

    // Format "x2" produit du hex minuscule sans dépendre de la locale
    private static string Hex(byte[] bytes) =>
        string.Concat(bytes.Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));

    internal static string ReadSecret(string path, string fallback) =>
        File.Exists(path) ? File.ReadAllText(path).Trim() : fallback;
}
