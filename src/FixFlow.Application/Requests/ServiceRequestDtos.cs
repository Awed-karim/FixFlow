using System.ComponentModel.DataAnnotations;

namespace FixFlow.Application.Requests;

public class CreateServiceRequestRequest
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }

    [Required]
    public int ServiceCategoryId { get; set; }
}

public class PhotoDto
{
    public string Url { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public class ServiceRequestDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Status { get; set; } = string.Empty;

    public int ServiceCategoryId { get; set; }
    public string ServiceCategoryName { get; set; } = string.Empty;

    public int? TechnicianProfileId { get; set; }
    public string? TechnicianName { get; set; }
    public string? TechnicianPhone { get; set; }

    public Guid TrackingToken { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public List<PhotoDto> Photos { get; set; } = new();
}

// النسخة العامة (بدون تسجيل دخول) لرابط التتبع: بيانات محدودة وآمنة
public class TrackingDto
{
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ServiceCategoryName { get; set; } = string.Empty;
    public string? TechnicianName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<PhotoDto> Photos { get; set; } = new();
}