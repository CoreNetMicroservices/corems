namespace CoreMs.TranslationMs.Core.Entities;

public class TranslationBundleEntity
{
    public long Id { get; set; }
    public string Realm { get; set; } = string.Empty;
    public string Lang { get; set; } = string.Empty;
    public Dictionary<string, string> Data { get; set; } = new();
    public DateTime UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}
