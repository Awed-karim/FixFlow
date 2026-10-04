using FixFlow.Domain.Common;

namespace FixFlow.Domain.Entities;

public class ServiceCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<TechnicianProfile> Technicians { get; set; } = new List<TechnicianProfile>();
    public ICollection<ServiceRequest> Requests { get; set; } = new List<ServiceRequest>();
}