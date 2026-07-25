namespace JwtAuthApp.Middleware
{
    public class AuthRedirectMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthRedirectMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/css") ||
                context.Request.Path.StartsWithSegments("/js") ||
                context.Request.Path.StartsWithSegments("/lib"))
            {
                await _next(context);
                return;
            }

            if (context.Request.Path.Equals("/Auth") ||
                context.Request.Path.Equals("/Auth/"))
            {
                context.Response.Redirect("/Auth/Login");
                return;
            }

            bool isLogoutPost = context.Request.Method == "POST" &&
                                context.Request.Path.Equals("/Auth/Logout");

            if (!context.User.Identity?.IsAuthenticated == true &&
                !context.Request.Path.StartsWithSegments("/Auth") &&
                !isLogoutPost)
            {
                context.Response.Redirect("/Auth/Login");
                return;
            }

            if (context.User.Identity?.IsAuthenticated == true &&
                context.Request.Path.StartsWithSegments("/Auth") &&
                !context.Request.Path.Equals("/Auth/Logout"))
            {
                context.Response.Redirect("/Home/Index");
                return;
            }

            await _next(context);
        }
    }
}
