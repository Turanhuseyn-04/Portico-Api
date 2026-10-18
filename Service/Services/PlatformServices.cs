using System.Net.Mail;
using Portico.Domain.Platform;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Admin;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Service.Services;

public class SettingService : ISettingService
{
    private const int MaxLength = 400;

    private readonly ISiteSettingRepository _settings;

    public SettingService(ISiteSettingRepository settings)
    {
        _settings = settings;
    }

    // Every known key, with its default when it has never been saved.
    public async Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken cancellationToken)
    {
        var saved = (await _settings.ListAsync(cancellationToken)).ToDictionary(setting => setting.Key, setting => setting.Value);
        return SiteSettingKeys.Defaults.ToDictionary(pair => pair.Key, pair => saved.TryGetValue(pair.Key, out var value) ? value : pair.Value);
    }

    // Only known keys are accepted, so the settings table cannot be used to store anything else.
    public async Task<IReadOnlyDictionary<string, string>> UpdateAsync(IReadOnlyDictionary<string, string> values, CancellationToken cancellationToken)
    {
        var unknown = values.Keys.Where(key => !SiteSettingKeys.Defaults.ContainsKey(key)).ToList();
        if (unknown.Count > 0) throw new BadRequestException($"Unknown setting: {string.Join(", ", unknown)}.");

        foreach (var (key, raw) in values)
        {
            var value = (raw ?? "").Trim();
            if (value.Length == 0) throw new BadRequestException($"{key} cannot be empty.");
            if (value.Length > MaxLength) throw new BadRequestException($"{key} must be under {MaxLength} characters.");
            if (key == SiteSettingKeys.SupportEmail && !(MailAddress.TryCreate(value, out var address) && address.Address == value))
                throw new BadRequestException("SupportEmail must be a valid email.");

            var setting = await _settings.FindAsync(key, cancellationToken);
            if (setting is null) await _settings.AddAsync(new SiteSetting { Key = key, Value = value }, cancellationToken);
            else setting.Value = value;
        }

        await _settings.SaveAsync(cancellationToken);
        return await GetAllAsync(cancellationToken);
    }
}

public class AuditService : IAuditService
{
    private readonly IAuditRepository _audit;

    public AuditService(IAuditRepository audit)
    {
        _audit = audit;
    }

    public async Task<IReadOnlyList<AuditEntryDto>> GetRecentAsync(int take, CancellationToken cancellationToken)
    {
        var rows = await _audit.ListAsync(Math.Clamp(take, 1, 500), cancellationToken);
        return rows.Select(entry => new AuditEntryDto(entry.Id, entry.At, entry.ActorEmail, entry.ActorRole, entry.Method, entry.Path, entry.StatusCode, entry.Detail)).ToArray();
    }

    public Task RecordAsync(AuditRecord record, CancellationToken cancellationToken)
    {
        return _audit.AddAsync(new AuditEntry
        {
            At = DateTime.UtcNow,
            ActorEmail = Clip(record.ActorEmail, 256),
            ActorRole = Clip(record.ActorRole, 20),
            Method = Clip(record.Method, 10),
            Path = Clip(record.Path, 400),
            StatusCode = record.StatusCode,
            Detail = record.Detail is null ? null : Clip(record.Detail, 400)
        }, cancellationToken);
    }

    private static string Clip(string value, int max) => value.Length <= max ? value : value[..max];
}
