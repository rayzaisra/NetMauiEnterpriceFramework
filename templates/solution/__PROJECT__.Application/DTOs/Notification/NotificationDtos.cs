namespace __PROJECT__.Application.DTOs.Notification;

public class NotificationDto
{
    public long Id { get; set; }
    public string? NotificationTypeId { get; set; }
    public string? Type { get; set; }
    public string? ShortText { get; set; }
    public string? LongText { get; set; }
    public string? ReffId { get; set; }
    public string? Href { get; set; }
    public bool IsSeen { get; set; }
    public DateTime? CreatedDate { get; set; }
}
