namespace __PROJECT__.Application.DTOs.Dashboard;

public sealed class DashboardWidgetDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? Value { get; set; }
    public string? Icon { get; set; }
    public string? ActionUrl { get; set; }
    public string? BackgroundColor { get; set; }
    public int SortOrder { get; set; }
}
