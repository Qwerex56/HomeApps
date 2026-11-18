using Devices.Domain.Capabilities;

namespace Devices.Application.Abstractions;

/// <summary>The namespaced capabilities registered in this instance (D14); changes are saved by the unit of work after the command.</summary>
public interface ICapabilityRegistry
{
    Task<RegisteredCapability?> FindAsync(string name, CancellationToken cancellationToken);

    Task<IReadOnlyList<RegisteredCapability>> ListAsync(CancellationToken cancellationToken);

    void Add(RegisteredCapability capability);
}
