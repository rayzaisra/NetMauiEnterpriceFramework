namespace __PROJECT__.Application.DTOs.General;

public class ClientErrorLogDto
{
    public string Source { get; set; } = "Client";
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string? UserId { get; set; }
    public string? Platform { get; set; }
}
