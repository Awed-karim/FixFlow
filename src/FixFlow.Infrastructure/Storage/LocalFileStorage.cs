using FixFlow.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace FixFlow.Infrastructure.Storage;

public class LocalFileStorage : IFileStorage
{
    private readonly FileStorageSettings _settings;

    public LocalFileStorage(IOptions<FileStorageSettings> options)
    {
        _settings = options.Value;
    }

    public async Task<string> SaveAsync(Stream content, string extension, string folder, CancellationToken ct = default)
    {
        var directory = Path.Combine(_settings.RootPath, folder);
        Directory.CreateDirectory(directory);

        // اسم عشوائي: مفيش تعارض، ومحدش يقدر يخمّن الرابط
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(directory, fileName);

        await using (var fileStream = new FileStream(fullPath, FileMode.Create))
        {
            await content.CopyToAsync(fileStream, ct);
        }

        return $"{_settings.RequestPath}/{folder.Replace('\\', '/')}/{fileName}";
    }
}