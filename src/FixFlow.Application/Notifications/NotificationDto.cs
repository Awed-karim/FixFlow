namespace FixFlow.Application.Notifications;

public class NotificationDto
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int? ServiceRequestId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}