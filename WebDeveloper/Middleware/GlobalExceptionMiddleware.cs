namespace WebDeveloper.Middleware
{
    /// <summary>
    /// Global exception handler middleware (tương đương GlobalHandler.java)
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (UnauthorizedAccessException ex)
            {
                context.Response.StatusCode = 401;
                context.Response.ContentType = "text/plain; charset=utf-8";
                await context.Response.WriteAsync(ex.Message);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
            {
                context.Response.StatusCode = 409;
                context.Response.ContentType = "text/plain; charset=utf-8";
                await context.Response.WriteAsync("Ca khám vừa được người khác đặt, vui lòng chọn ca khác!");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business logic error");
                context.Response.StatusCode = 400;
                context.Response.ContentType = "text/plain; charset=utf-8";
                await context.Response.WriteAsync(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");
                context.Response.StatusCode = 500;
                context.Response.ContentType = "text/plain; charset=utf-8";
                await context.Response.WriteAsync("Đã xảy ra lỗi hệ thống!");
            }
        }
    }
}
