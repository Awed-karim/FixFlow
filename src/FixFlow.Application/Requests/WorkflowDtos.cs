using System.ComponentModel.DataAnnotations;

namespace FixFlow.Application.Requests;

public class RejectRequestRequest
{
    [MaxLength(300)]
    public string? Reason { get; set; }
}

public class ReviewRequest
{
    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }
}

// ملف مرفوع، من غير ما الـ Application يعرف حاجة عن ASP.NET (IFormFile)
public record FileUpload(Stream Content, string FileName, long Length, string ContentType);