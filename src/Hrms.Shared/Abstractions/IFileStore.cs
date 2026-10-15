namespace Hrms.Shared.Abstractions;

public interface IFileStore
{
    Task<string> SaveAsync(Stream stream, string relativePath, string fileName, CancellationToken cancellationToken = default);
    Task DeleteAsync(string fileKey, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string fileKey, CancellationToken cancellationToken = default);
}
