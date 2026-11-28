using System.Security.Cryptography;
using Jarvis.Domain.Settings;
using Jarvis.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Jarvis.Infrastructure.Brain;

/// <summary>
/// The model connection (D14): the admin's settings from the database win, <c>Jarvis:OpenAi</c> from the environment
/// is the fallback. The key is protected with Data Protection and is never returned.
/// </summary>
public sealed class LlmSettingsStore(JarvisDbContext db, IDataProtectionProvider dataProtection, IOptions<OpenAiChatOptions> environment) : ILlmConnection
{
    public const int MaxTimeoutSeconds = 120;
    private const string Purpose = "Jarvis.LlmSettings.ApiKey";

    private IDataProtector Protector => dataProtection.CreateProtector(Purpose);

    public async Task<LlmSettingsView> GetAsync(CancellationToken cancellationToken)
    {
        var current = await CurrentAsync(cancellationToken);
        return new LlmSettingsView(current.BaseUrl, current.Model, !string.IsNullOrWhiteSpace(current.ApiKey), (int)current.Timeout.TotalSeconds);
    }

    public async Task<OpenAiChatOptions> CurrentAsync(CancellationToken cancellationToken)
    {
        var env = environment.Value;
        var row = await db.LlmSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Id == JarvisLlmSettings.SingleId, cancellationToken);
        if (row is null)
        {
            return env;
        }

        return new OpenAiChatOptions
        {
            BaseUrl = row.BaseUrl,
            Model = row.Model,
            ApiKey = Unprotect(row.ApiKeyProtected),
            Timeout = TimeSpan.FromSeconds(row.TimeoutSeconds),
            MaxTurns = env.MaxTurns,
            GatewayUrl = env.GatewayUrl,
        };
    }

    /// <summary><paramref name="apiKey"/>: <c>null</c> keeps the stored key, an empty string removes it.</summary>
    public async Task SaveAsync(string baseUrl, string model, string? apiKey, int timeoutSeconds, CancellationToken cancellationToken)
    {
        var row = await db.LlmSettings.FirstOrDefaultAsync(s => s.Id == JarvisLlmSettings.SingleId, cancellationToken);
        if (row is null)
        {
            row = new JarvisLlmSettings();
            db.LlmSettings.Add(row);
        }

        row.Set(baseUrl.Trim(), model.Trim(), timeoutSeconds);
        if (apiKey is not null)
        {
            row.SetApiKey(string.IsNullOrWhiteSpace(apiKey) ? null : Protector.Protect(apiKey.Trim()));
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private string? Unprotect(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        try
        {
            return Protector.Unprotect(value);
        }
        catch (CryptographicException)
        {
            // The keys were lost: as if there were no key, the admin sets it again.
            return null;
        }
    }
}
