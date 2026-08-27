namespace JwtAuthApp.Middleware
{
    public class SessionTokenMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionTokenMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var token = context.Session.GetString("JWToken");
            if (!string.IsNullOrEmpty(token) &&
                !context.Request.Headers.ContainsKey("Authorization"))
            {
                // Не затираем Authorization, присланный клиентом (например, API-запрос с собственным Bearer)
                context.Request.Headers["Authorization"] = "Bearer " + token;
            }
            await _next(context);
        }
    }
}
