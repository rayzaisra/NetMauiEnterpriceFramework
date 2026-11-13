namespace __PROJECT__.Application.DTOs.General;

public sealed class ApiActivityLogEntry
{
    public string? UserId { get; set; }
    public string? IpAddress { get; set; }
    public string? HttpMethod { get; set; }
    public string? Path { get; set; }
    public string? RouteTemplate { get; set; }
    public int StatusCode { get; set; }
    public bool Success { get; set; }
    public int DurationMs { get; set; }
    public string? CorrelationId { get; set; }
    public string? UserAgent { get; set; }
    public string? ClientType { get; set; }
    public string? ErrorCode { get; set; }
    public DateTime CreatedDate { get; set; }
}
