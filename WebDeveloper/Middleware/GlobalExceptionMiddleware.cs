using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WebDeveloper.Exceptions;

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
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/problem+json";

            var problemDetails = new ProblemDetails
            {
                Instance = context.Request.Path
            };

            switch (ex)
            {
                case DomainException domainEx:
                    context.Response.StatusCode = domainEx.StatusCode;
                    problemDetails.Status = domainEx.StatusCode;
                    problemDetails.Title = "Lỗi nghiệp vụ hệ thống";
                    problemDetails.Detail = domainEx.Message;
                    problemDetails.Extensions["errorCode"] = domainEx.ErrorCode;
                    break;

                case UnauthorizedAccessException:
                    context.Response.StatusCode = 401;
                    problemDetails.Status = 401;
                    problemDetails.Title = "Không có quyền truy cập";
                    problemDetails.Detail = ex.Message;
                    problemDetails.Extensions["errorCode"] = "UNAUTHORIZED";
                    break;

                case Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException:
                    context.Response.StatusCode = 409;
                    problemDetails.Status = 409;
                    problemDetails.Title = "Xung đột dữ liệu";
                    problemDetails.Detail = "Ca khám vừa được người khác đặt, vui lòng chọn ca khác!";
                    problemDetails.Extensions["errorCode"] = "CONCURRENCY_CONFLICT";
                    break;

                case InvalidOperationException invalidOpEx:
                    _logger.LogWarning(invalidOpEx, "Business logic error");
                    context.Response.StatusCode = 400;
                    problemDetails.Status = 400;
                    problemDetails.Title = "Thao tác không hợp lệ";
                    problemDetails.Detail = invalidOpEx.Message;
                    problemDetails.Extensions["errorCode"] = "INVALID_OPERATION";
                    break;

                default:
                    _logger.LogError(ex, "Unhandled exception");
                    context.Response.StatusCode = 500;
                    problemDetails.Status = 500;
                    problemDetails.Title = "Lỗi máy chủ nội bộ";
                    problemDetails.Detail = "Đã xảy ra lỗi hệ thống!";
                    problemDetails.Extensions["errorCode"] = "INTERNAL_SERVER_ERROR";
                    break;
            }

            var json = JsonSerializer.Serialize(problemDetails);
            await context.Response.WriteAsync(json);
        }
    }
}
