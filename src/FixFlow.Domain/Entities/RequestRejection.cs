using FixFlow.Domain.Common;

namespace FixFlow.Domain.Entities;

public class RequestRejection : BaseEntity
{
    public int ServiceRequestId { get; set; }
    public ServiceRequest ServiceRequest { get; set; } = null!;

    public int TechnicianProfileId { get; set; }
    public TechnicianProfile TechnicianProfile { get; set; } = null!;

    public string? Reason { get; set; }
}