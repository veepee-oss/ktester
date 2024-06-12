namespace KafkaTester.Model;

public class FilterSettingsModel
{
    public string Text { get; set; } = string.Empty;

    public bool IsInvariantCase { get; set; } = false;
    public bool IsCheckKey { get; set; } = true;
    public bool IsCheckMessage { get; set; } = true;

    public bool IsFiltering() => !string.IsNullOrWhiteSpace(Text);
}
