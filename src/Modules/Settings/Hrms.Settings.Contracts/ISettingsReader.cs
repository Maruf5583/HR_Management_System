namespace Hrms.Settings.Contracts;

public interface ISettingsReader
{
    Task<string> GetStringAsync(string key, CancellationToken cancellationToken = default);
    Task<int> GetIntAsync(string key, CancellationToken cancellationToken = default);
    Task<decimal> GetDecimalAsync(string key, CancellationToken cancellationToken = default);
}
