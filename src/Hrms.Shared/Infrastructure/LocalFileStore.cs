using Hrms.Shared.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Hrms.Shared.Infrastructure;

public class LocalFileStore : IFileStore
{
    private readonly string _rootPath;

    public LocalFileStore(IConfiguration configuration)
    {
        _rootPath = configuration["FileStorage:RootPath"] ?? Path.Combine("wwwroot", "uploads");
    }

    public async Task<string> SaveAsync(Stream stream, string relativePath, string fileName, CancellationToken cancellationToken = default)
    {
        var targetDir = Path.Combine(_rootPath, relativePath);
        Directory.CreateDirectory(targetDir);

        var fullPath = Path.Combine(targetDir, fileName);
        using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
        {
            await stream.CopyToAsync(fileStream, cancellationToken);
        }

        var key = Path.Combine(relativePath, fileName).Replace('\\', '/');
        return key;
    }

    public Task DeleteAsync(string fileKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileKey))
        {
            return Task.CompletedTask;
        }

        var fullPath = Path.Combine(_rootPath, fileKey.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string fileKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileKey))
        {
            return Task.FromResult(false);
        }

        var fullPath = Path.Combine(_rootPath, fileKey.Replace('/', Path.DirectorySeparatorChar));
        return Task.FromResult(File.Exists(fullPath));
    }
}
