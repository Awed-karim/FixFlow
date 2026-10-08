namespace FixFlow.Application.Interfaces;

public interface IFileStorage
{
    // بيحفظ الملف ويرجّع الرابط (URL) اللي يتخزن في الداتابيز
    Task<string> SaveAsync(Stream content, string extension, string folder, CancellationToken ct = default);
}