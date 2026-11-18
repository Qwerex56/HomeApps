using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.Capabilities.ListRegisteredCapabilities;

/// <summary>The namespaced capabilities that devices brought into this instance (D14), for the server's Owner only.</summary>
public sealed record ListRegisteredCapabilitiesQuery : IQuery<IReadOnlyList<RegisteredCapabilityView>>;
