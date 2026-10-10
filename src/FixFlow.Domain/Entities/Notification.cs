using System.ComponentModel.DataAnnotations;
using FixFlow.Domain.Common;

namespace FixFlow.Domain.Entities;

public class Notification : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    // نوع الحدث (Assigned, Accepted, ...)
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    // مرجع ناعم للطلب (من غير Foreign Key)
    public int? ServiceRequestId { get; set; }

    public bool IsRead { get; set; }
}