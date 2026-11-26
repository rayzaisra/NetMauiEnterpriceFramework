namespace __PROJECT__.Shared.Models;

public class AppTabDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string? ComponentType { get; set; }
    public string? Url { get; set; }
    public string? Icon { get; set; }
    public Dictionary<string, object>? Parameters { get; set; }
    public string? TitleKey { get; set; }
    public Guid RefreshKey { get; set; } = Guid.NewGuid();
}
