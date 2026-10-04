using FixFlow.Domain.Common;
using FixFlow.Domain.Enums;

namespace FixFlow.Domain.Entities;

public class RequestPhoto : BaseEntity
{
    public int ServiceRequestId { get; set; }
    public ServiceRequest ServiceRequest { get; set; } = null!;

    public string Url { get; set; } = string.Empty;
    public PhotoType Type { get; set; }
}