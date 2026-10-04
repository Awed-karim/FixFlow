using FixFlow.Domain.Common;

namespace FixFlow.Domain.Entities;

public class Review : BaseEntity
{
    public int ServiceRequestId { get; set; }
    public ServiceRequest ServiceRequest { get; set; } = null!;

    public int Rating { get; set; }   // من 1 إلى 5
    public string? Comment { get; set; }
}