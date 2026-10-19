using Microsoft.AspNetCore.Mvc;

namespace WebDeveloper.Helpers;

public static class ApiValidationMessages
{
    public static IActionResult InvalidRequest(ActionContext context)
    {
        var errors = context.ModelState.Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(entry => entry.Key, entry => entry.Value!.Errors.Select(error =>
                !string.IsNullOrWhiteSpace(error.ErrorMessage) &&
                !System.Text.RegularExpressions.Regex.IsMatch(error.ErrorMessage, @"(?i)^(the |a value|a non-empty)|is not valid|is invalid|could not be converted") &&
                System.Text.RegularExpressions.Regex.IsMatch(error.ErrorMessage, "[À-ỹĐđ]")
                    ? error.ErrorMessage
                    : "Thông tin chưa hợp lệ. Vui lòng kiểm tra lại các trường đã nhập.").ToArray());
        return new BadRequestObjectResult(new ValidationProblemDetails(errors)
        {
            Status = 400,
            Title = "Thông tin chưa hợp lệ",
            Detail = "Vui lòng kiểm tra lại các trường đã nhập.",
            Instance = context.HttpContext.Request.Path
        });
    }
}
