namespace WebDeveloper.Helpers;

public static class PatientRelationship
{
    public static IReadOnlyList<string> Options { get; } = Array.AsReadOnly(new[]
    {
        "Ông", "Bà", "Bố", "Mẹ", "Vợ", "Chồng", "Con", "Anh", "Chị", "Em", "Cháu", "Chắt"
    });

    public static string Validate(string? value, string? existing = null)
    {
        var relationship = value?.Trim() ?? "";
        // Preserve existing self profiles and legacy labels when editing other fields.
        if (existing == "SELF")
        {
            if (relationship != "SELF") throw new InvalidOperationException("Không thể thay đổi mối quan hệ của hồ sơ bản thân.");
            return "SELF";
        }
        if (Options.Contains(relationship) || (!string.IsNullOrEmpty(existing) && relationship == existing))
            return relationship;
        throw new InvalidOperationException("Vui lòng chọn mối quan hệ trong danh sách.");
    }
}
