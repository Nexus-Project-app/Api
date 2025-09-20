namespace Nexus.Api.Middlewares
{
    public class SecureHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecureHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

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

            await _next(context);
        }
    }

    // Extension pour plus de lisibilité
    public static class SecureHeadersMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecureHeaders(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SecureHeadersMiddleware>();
        }
    }
}

