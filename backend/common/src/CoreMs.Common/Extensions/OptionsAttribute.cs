namespace CoreMs.Common.Extensions;

/// <summary>
/// Marks a class for automatic options registration via assembly scanning.
///
/// The config section name is resolved in this order:
///   1. The <paramref name="sectionName"/> passed to this attribute, if set.
///   2. A <c>public const string SectionName</c> field on the class, if present.
///   3. The class name with a trailing "Options"/"Option" removed (e.g. JwtOptions -> "Jwt").
///
/// Always bound with DataAnnotation validation and validated on startup. Classes without any
/// DataAnnotation attributes simply pass validation, so no opt-out is needed.
///
/// Usage:
///   [Options]           // derive section from name
///   [Options("Mail")]   // custom section name
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class OptionsAttribute(string? sectionName = null) : Attribute
{
    public string? SectionName { get; } = sectionName;
}
