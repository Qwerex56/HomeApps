using Devices.Application.Common;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.Tools.JarvisDevices;

/// <summary>
/// The household's smart-home and assistant devices as JARVIS's prompt section needs them (H-3c). Not a person's request: JARVIS's
/// service client asks on the internal endpoint, which has already checked its secret.
/// </summary>
public sealed record GetJarvisDevicesQuery(Guid HouseholdId) : IQuery<IReadOnlyList<DeviceDescriptionView>>;
