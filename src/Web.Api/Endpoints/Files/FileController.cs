using Application.Abstractions.Storage;
using Microsoft.AspNetCore.Mvc;

namespace Web.Api.Endpoints.Files;

/// <summary>
/// Proxy pour servir les fichiers stockés dans Garage.
/// L'API s'authentifie auprès de Garage, puis sert le fichier au client.
/// Permet l'accès public sans exposer les credentials.
/// </summary>
[ApiController]
[Route("api/files")]
public class FileController : ControllerBase
{
    private readonly IAttachmentStorage _storage;
    private readonly ILogger<FileController> _logger;

    public FileController(IAttachmentStorage storage, ILogger<FileController> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    /// <summary>
    /// Récupère un fichier depuis le stockage Garage et le sert au client.
    /// 
    /// Exemple d'URL:
    /// GET /api/files/posts/6a412367-17f4-4b60-b5a9-fb4ed0eab7b4/aec5a52c-8b52-4d86-a098-39faff94449b.png
    /// </summary>
    /// <param name="path">Chemin du fichier dans le bucket Garage (avec les dossiers)</param>
    /// <returns>Le contenu du fichier avec le type MIME approprié</returns>
    [HttpGet("{**path}")]
    public async Task<IActionResult> GetFile(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                _logger.LogWarning("Tentative d'accès à un fichier avec un chemin vide");
                return BadRequest("Le chemin du fichier est requis");
            }

            _logger.LogInformation("Récupération du fichier: {Path}", path);

            // Récupère le fichier depuis Garage
            var stream = await _storage.DownloadAsync(path);

            // Détermine le type MIME en fonction de l'extension
            var contentType = GetContentType(path);

            _logger.LogInformation("Fichier récupéré avec succès: {Path}", path);

            // Sert le fichier avec support du range processing (pour les vidéos, etc.)
            return File(stream, contentType, enableRangeProcessing: true);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning(ex, "Fichier non trouvé: {Path}", path);
            return NotFound($"Le fichier '{path}' n'existe pas");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Accès non autorisé au fichier: {Path}", path);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération du fichier: {Path}", path);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                "Erreur lors de la récupération du fichier");
        }
    }

    /// <summary>
    /// Détermine le type MIME en fonction de l'extension du fichier.
    /// Utilise ToUpperInvariant pour la comparaison.
    /// </summary>
    private static string GetContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToUpperInvariant();

        return extension switch
        {
            // Images
            ".JPG" or ".JPEG" => "image/jpeg",
            ".PNG" => "image/png",
            ".GIF" => "image/gif",
            ".WEBP" => "image/webp",
            ".SVG" => "image/svg+xml",
            ".ICO" => "image/x-icon",

            // Documents
            ".PDF" => "application/pdf",
            ".DOC" => "application/msword",
            ".DOCX" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".XLS" => "application/vnd.ms-excel",
            ".XLSX" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".PPT" => "application/vnd.ms-powerpoint",
            ".PPTX" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".TXT" => "text/plain",
            ".CSV" => "text/csv",

            // Audio/Video
            ".MP3" => "audio/mpeg",
            ".MP4" => "video/mp4",
            ".WEBM" => "video/webm",
            ".WAV" => "audio/wav",
            ".M4A" => "audio/mp4",

            // Archives
            ".ZIP" => "application/zip",
            ".RAR" => "application/x-rar-compressed",
            ".7Z" => "application/x-7z-compressed",

            // Default
            _ => "application/octet-stream"
        };
    }
}
