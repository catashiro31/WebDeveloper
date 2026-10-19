using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
namespace WebDeveloper.Helpers;

public static class DoctorDegree
{
    public static readonly string[] Options = ["Bác sĩ", "Thạc sĩ", "Tiến sĩ", "Bác sĩ chuyên khoa I", "Bác sĩ chuyên khoa II", "Phó giáo sư", "Giáo sư"];
    public static bool IsValid(string? value)
    {
        var text = value?.Trim() ?? "";
        if (Options.Contains(text)) return true;
        if (!text.StartsWith("Khác: ", StringComparison.Ordinal) || text.Length > 100) return false;
        var detail = text[6..].Trim();
        return detail.Length >= 3 && detail.Count(char.IsLetter) >= 3 && Regex.IsMatch(detail, @"^[\p{L}\p{M}\p{N}\s.,()/'-]+$");
    }
}
public sealed class DoctorDegreeAttribute : ValidationAttribute
{
    public DoctorDegreeAttribute() => ErrorMessage = "Vui lòng chọn học vị/bằng cấp hợp lệ; mục Khác cần mô tả bằng chữ (tối đa 100 ký tự).";
    public override bool IsValid(object? value) => DoctorDegree.IsValid(value as string);
}
