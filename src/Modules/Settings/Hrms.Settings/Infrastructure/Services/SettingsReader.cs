using System.Globalization;
using Hrms.Settings.Application.Abstractions;
using Hrms.Settings.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Settings.Infrastructure.Services;

public class SettingsReader : ISettingsReader
{
    private readonly ISettingsDbContext _context;

    public SettingsReader(ISettingsDbContext context)
    {
        _context = context;
    }

    public async Task<string> GetStringAsync(string key, CancellationToken cancellationToken = default)
    {
        var setting = await _context.Settings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SettingKey == key, cancellationToken);

        if (setting == null)
        {
            throw new InvalidOperationException($"Setting with key '{key}' was not found.");
        }

        return setting.ValueText;
    }

    public async Task<int> GetIntAsync(string key, CancellationToken cancellationToken = default)
    {
        var valueText = await GetStringAsync(key, cancellationToken);
        if (!int.TryParse(valueText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            throw new FormatException($"Setting with key '{key}' has value '{valueText}' which cannot be parsed as an integer.");
        }

        return result;
    }

    public async Task<decimal> GetDecimalAsync(string key, CancellationToken cancellationToken = default)
    {
        var valueText = await GetStringAsync(key, cancellationToken);
        if (!decimal.TryParse(valueText, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
        {
            throw new FormatException($"Setting with key '{key}' has value '{valueText}' which cannot be parsed as a decimal.");
        }

        return result;
    }
}
