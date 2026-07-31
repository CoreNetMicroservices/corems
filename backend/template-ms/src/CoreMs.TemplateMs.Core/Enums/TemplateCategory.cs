namespace CoreMs.TemplateMs.Core.Enums;

public static class TemplateCategory
{
    public const string Common = "COMMON";
    public const string Email = "EMAIL";
    public const string Sms = "SMS";
    public const string Document = "DOCUMENT";

    public static readonly IReadOnlyList<string> All = [Common, Email, Sms, Document];

    public static bool IsValid(string category) => All.Contains(category, StringComparer.OrdinalIgnoreCase);
}
