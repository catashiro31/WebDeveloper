using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebDeveloper.Data;

namespace WebDeveloper.Security
{
    /// <summary>
    /// JWT Middleware - kiểm tra token blacklist và account status
    /// Tương đương JwtTokenFilter.java
    /// </summary>
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;

        public JwtMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        private async Task<bool> ContinueAsGuest(HttpContext context)
        {
            foreach (var name in new[] { "accessToken", "refreshToken", "user_info" })
                context.Response.Cookies.Delete(name, new CookieOptions { Path = "/" });
            if (context.Request.Path.StartsWithSegments("/api")) return false;
            context.User = new ClaimsPrincipal(new ClaimsIdentity());
            if (context.Request.Path.StartsWithSegments("/Admin") || context.Request.Path.StartsWithSegments("/Doctor") || context.Request.Path.StartsWithSegments("/Patient"))
                context.Response.Redirect("/Auth/Login");
            else await _next(context);
            return true;
        }

        public async Task InvokeAsync(HttpContext context, ApplicationDbContext db)
        {
            // Bỏ qua các endpoint public
            var path = context.Request.Path.Value ?? "";
            if (path.StartsWith("/api/v1/auth/"))
            {
                await _next(context);
                return;
            }

            if (path.StartsWith("/api/v1/portal/"))
            {
                await _next(context);
                return;
            }

            // Kiểm tra nếu user đã authenticated (JWT Bearer đã validate)
            if (context.User.Identity?.IsAuthenticated == true)
            {
                // Authentication accepts a cookie or a bearer header. Apply revocation
                // and account checks to the token that authenticated this request.
                var token = context.Items["AuthenticatedAccessToken"] as string ?? context.Request.Cookies["accessToken"];
                if (string.IsNullOrEmpty(token))
                {
                    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                    if (authHeader != null && authHeader.StartsWith("Bearer "))
                        token = authHeader["Bearer ".Length..];
                }

                if (!string.IsNullOrEmpty(token))
                {
                    // Kiểm tra token blacklist
                    var tokenKey = TokenBlacklistKey.FromToken(token);
                    // Keep recognizing tokens revoked before hashed keys were introduced.
                    var isBlacklisted = await db.TokenBlacklists.AnyAsync(t => t.Token == tokenKey || t.Token == token);
                    if (isBlacklisted)
                    {
                        if (await ContinueAsGuest(context)) return;
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json; charset=utf-8";
                        await context.Response.WriteAsync("{\"error\": \"Token đã bị vô hiệu hóa. Vui lòng đăng nhập lại.\"}");
                        return;
                    }

                    // Kiểm tra tài khoản có bị khóa không
                    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                    if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
                    {
                        var user = await db.Users.FindAsync(userId);
                        if (user != null && user.IsActive != true)
                        {
                            if (await ContinueAsGuest(context)) return;
                            context.Response.StatusCode = 403;
                            context.Response.ContentType = "application/json; charset=utf-8";
                            await context.Response.WriteAsync("{\"error\": \"Tài khoản của bạn đã bị khóa.\"}");
                            return;
                        }
                    }
                }
            }

            await _next(context);
        }
    }
}
