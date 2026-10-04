using FixFlow.Domain.Common;
using FixFlow.Domain.Enums;

namespace FixFlow.Domain.Entities;

public class ServiceRequest : BaseEntity
{
    public string CustomerId { get; set; } = string.Empty;   // هيتربط بـ Identity في المرحلة 2
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    public int ServiceCategoryId { get; set; }
    public ServiceCategory ServiceCategory { get; set; } = null!;

    public int? TechnicianProfileId { get; set; }
    public TechnicianProfile? TechnicianProfile { get; set; }

    public DateTime? AssignedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // رابط التتبع (Quick Share) بدون تسجيل دخول
    public Guid TrackingToken { get; set; } = Guid.NewGuid();

    public ICollection<RequestPhoto> Photos { get; set; } = new List<RequestPhoto>();
    public Review? Review { get; set; }
}