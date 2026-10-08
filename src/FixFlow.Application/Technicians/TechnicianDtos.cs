using System.ComponentModel.DataAnnotations;

namespace FixFlow.Application.Technicians;

public class TechnicianDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public int ServiceCategoryId { get; set; }
    public string ServiceCategoryName { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsAvailable { get; set; }
    public double AverageRating { get; set; }
    public int CompletedJobsCount { get; set; }
}

public class UpsertTechnicianProfileRequest
{
    [Required]
    public int ServiceCategoryId { get; set; }

    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }

    public bool IsAvailable { get; set; } = true;
}

public class SetAvailabilityRequest
{
    public bool IsAvailable { get; set; }
}