namespace CoreMs.Common.Extensions;

/// <summary>
/// Marks a class for automatic options registration via assembly scanning.
///
/// The config section name is resolved in this order:
///   1. The <paramref name="sectionName"/> passed to this attribute, if set.
///   2. A <c>public const string SectionName</c> field on the class, if present.
///   3. The class name with a trailing "Options"/"Option" removed (e.g. JwtOptions -> "Jwt").
///
/// Bound with DataAnnotation validation unless <see cref="Validate"/> is set to <c>false</c>.
///
/// Usage:
///   [Options]                          // derive section from name, validate
///   [Options(Validate = false)]        // derive section from name, binding check only
///   [Options("Mail")]                  // custom section, validate
///   [Options("Sms", Validate = false)] // custom section, binding check only
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class OptionsAttribute(string? sectionName = null) : Attribute
{
    public string? SectionName { get; } = sectionName;
    public bool Validate { get; init; } = true;
}
