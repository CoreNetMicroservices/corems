namespace CoreMs.Common.Contracts;

/// <summary>
/// Command published to the message bus to request an email be sent via Communication MS.
/// </summary>
public record SendEmailCommand(
    string To,
    string Subject,
    string TemplateName,
    Dictionary<string, string> TemplateData);
