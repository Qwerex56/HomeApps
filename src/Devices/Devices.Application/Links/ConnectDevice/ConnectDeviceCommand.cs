using Devices.Application.Common;
using Devices.Domain.Devices;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.Links.ConnectDevice;

/// <summary>
/// A device of mine connected to <c>/devices/link</c> and said who it is and what it can do. The first connection under
/// a name adds the device; later ones replace its actions ("Nie pytaj" stays per action name). A removed device is refused.
/// A <c>homeapps.device/1</c> descriptor in the hello replaces the stored one; without it the device is a legacy one and
/// its purpose follows its kind. A paired household satellite connects with its own token (S13) and keeps the name its household gave it.
/// </summary>
public sealed record ConnectDeviceCommand(string Name, DeviceKind Kind, IReadOnlyList<ActionInput> Actions, string? Descriptor = null)
    : ICommand<Guid>;
