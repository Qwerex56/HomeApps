using Microsoft.EntityFrameworkCore;

namespace HomeApps.Plugins.Hosting;

internal sealed class PluginEnablementStore(PluginDbContext db) : IPluginEnablement
{
    public Task<bool> IsEnabledAsync(Guid householdId, CancellationToken cancellationToken) =>
        db.PluginEnablements.AnyAsync(e => e.HouseholdId == householdId && e.Enabled, cancellationToken);
}
