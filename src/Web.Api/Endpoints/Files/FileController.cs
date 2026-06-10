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
    [ProduceResponseType(StatusCodes.Status200OK)]
    [ProduceResponseType(StatusCodes.Status404NotFound)]
    [ProduceResponseType(StatusCodes.Status500InternalServerError)]
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
        catch (FileNotFoundException)
        {
            _logger.LogWarning("Fichier non trouvé: {Path}", path);
            return NotFound($"Le fichier '{path}' n'existe pas");
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Accès non autorisé au fichier: {Path}", path);
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
    /// </summary>
    private static string GetContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            // Images
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".ico" => "image/x-icon",

            // Documents
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".txt" => "text/plain",
            ".csv" => "text/csv",

            // Audio/Video
            ".mp3" => "audio/mpeg",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".wav" => "audio/wav",
            ".m4a" => "audio/mp4",

            // Archives
            ".zip" => "application/zip",
            ".rar" => "application/x-rar-compressed",
            ".7z" => "application/x-7z-compressed",

            // Default
            _ => "application/octet-stream"
        };
    }
}
