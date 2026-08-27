namespace JwtAuthApp.Middleware
{
    // Добавляет базовые security-заголовки ко всем ответам
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var headers = context.Response.Headers;

            // Запрет встраивания страниц в iframe (защита от clickjacking)
            headers["X-Frame-Options"] = "DENY";
            // Запрет MIME-sniffing
            headers["X-Content-Type-Options"] = "nosniff";
            // Не передавать referrer внешним сайтам
            headers["Referrer-Policy"] = "no-referrer";
            // Отключение лишних браузерных фич
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

            // CSP: разрешаем jsdelivr (bootstrap-icons), inline-скрипты/стили оставлены,
            // т.к. Razor-вьюхи используют inline-блоки; 'unsafe-eval' нужен jquery.validate в некоторых версиях
            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
                "font-src 'self' https://cdn.jsdelivr.net; " +
                "img-src 'self' data:; " +
                "frame-ancestors 'none'; " +
                "form-action 'self'; " +
                "base-uri 'self'";

            await _next(context);
        }
    }
}
