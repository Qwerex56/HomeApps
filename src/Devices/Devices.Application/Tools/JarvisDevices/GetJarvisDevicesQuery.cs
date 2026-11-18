using Devices.Application.Common;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.Tools.JarvisDevices;

/// <summary>
/// The household's smart-home and assistant devices as JARVIS's prompt section needs them (H-3c). Not a person's request: JARVIS's
/// service client asks on the internal endpoint, which has already checked its secret. The answer is for <c>UserId</c> only: a satellite of the household or one of its members, else empty.
/// </summary>
public sealed record GetJarvisDevicesQuery(Guid HouseholdId, Guid UserId) : IQuery<IReadOnlyList<DeviceDescriptionView>>;
