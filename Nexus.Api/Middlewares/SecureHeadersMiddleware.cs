/// <summary>
/// Middleware chargé d’ajouter des en-têtes HTTP de sécurité
/// afin de renforcer la protection de l’application.
/// </summary>
public class SecureHeadersMiddleware
{
    private readonly RequestDelegate next;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="SecureHeadersMiddleware"/>.
    /// </summary>
    /// <param name="next">Délégué représentant la requête suivante dans le pipeline.</param>
    public SecureHeadersMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    /// <summary>
    /// Invoque le middleware et applique les en-têtes HTTP de sécurité
    /// à la réponse en cours de traitement.
    /// </summary>
    /// <param name="context">Contexte HTTP de la requête en cours.</param>
    /// <returns>Tâche asynchrone représentant l’exécution du middleware.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Transport / HTTPS (HSTS déjà géré via app.UseHsts())

        // 1. Empêcher le sniffing MIME
        headers["X-Content-Type-Options"] = "nosniff";

        // 2. Anti clickjacking
        headers["X-Frame-Options"] = "DENY";

        // 3. Referrer Policy
        headers["Referrer-Policy"] = "no-referrer";

        // 4. Permissions Policy (désactiver APIs sensibles du navigateur)
        headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

        // 5. Legacy Adobe (Flash/PDF)
        headers["X-Permitted-Cross-Domain-Policies"] = "none";

        // 6. Désactiver le cache (utile pour JWT / données sensibles)
        headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        headers["Pragma"] = "no-cache";
        headers["Expires"] = "0";

        await next(context);
    }
}

/// <summary>
/// Classe d’extensions permettant d’ajouter le middleware
/// <see cref="SecureHeadersMiddleware"/> dans le pipeline HTTP.
/// </summary>
public static class SecureHeadersMiddlewareExtensions
{
    /// <summary>
    /// Ajoute le middleware <see cref="SecureHeadersMiddleware"/> 
    /// au pipeline des requêtes HTTP.
    /// </summary>
    /// <param name="builder">Application builder utilisé pour configurer le pipeline.</param>
    /// <returns>Instance de <see cref="IApplicationBuilder"/> pour chaînage.</returns>
    public static IApplicationBuilder UseSecureHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecureHeadersMiddleware>();
    }
}