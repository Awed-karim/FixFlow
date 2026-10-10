namespace FixFlow.Application.Requests;

public class RequestUpdateMessage
{
    public int RequestId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Event { get; set; } = string.Empty;
    public string? TechnicianName { get; set; }
    public DateTime UpdatedAt { get; set; }
}