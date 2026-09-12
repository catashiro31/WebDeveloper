using System.Security.Claims;
using WebDeveloper.Data;
using WebDeveloper.Models.Entities;

namespace WebDeveloper.Helpers
{
    /// <summary>
    /// Lấy current user từ HttpContext (tương đương docbooking.utils.Security trong Java)
    /// </summary>
    public static class SecurityHelper
    {
        public static int GetCurrentUserId(HttpContext httpContext)
        {
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedAccessException("Không thể xác định người dùng hiện tại! Vui lòng đăng nhập lại.");

            return int.Parse(userIdClaim.Value);
        }

        public static async Task<User> GetCurrentUser(HttpContext httpContext, ApplicationDbContext db)
        {
            var userId = GetCurrentUserId(httpContext);
            var user = await db.Users.FindAsync(userId);
            if (user == null)
                throw new UnauthorizedAccessException("Không thể xác định người dùng hiện tại! Vui lòng đăng nhập lại.");
            return user;
        }
    }
}
