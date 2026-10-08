namespace FixFlow.Infrastructure.Storage;

public class FileStorageSettings
{
    // مكان الحفظ على الجهاز (هيتحوّل لمسار كامل جوه فولدر الـ API في Program.cs)
    public string RootPath { get; set; } = "uploads";

    // الجزء اللي بيظهر في الرابط: /uploads/...
    public string RequestPath { get; set; } = "/uploads";
}