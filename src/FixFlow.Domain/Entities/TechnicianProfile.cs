using FixFlow.Domain.Common;

namespace FixFlow.Domain.Entities;

public class TechnicianProfile : BaseEntity
{
    public string UserId { get; set; } = string.Empty;   // هيتربط بـ Identity في المرحلة 2
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;

    public int ServiceCategoryId { get; set; }
    public ServiceCategory ServiceCategory { get; set; } = null!;

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public bool IsAvailable { get; set; } = true;
    public double AverageRating { get; set; }
    public int CompletedJobsCount { get; set; }

    public ICollection<ServiceRequest> Requests { get; set; } = new List<ServiceRequest>();
}