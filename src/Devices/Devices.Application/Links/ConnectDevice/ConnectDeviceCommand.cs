using Devices.Application.Common;
using Devices.Domain.Devices;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.Links.ConnectDevice;

/// <summary>
/// A device of mine connected to <c>/devices/link</c> and said who it is and what it can do. The first connection under
/// a name adds the device; later ones replace its actions ("Nie pytaj" stays per action name). A removed device is refused.
/// </summary>
public sealed record ConnectDeviceCommand(string Name, DeviceKind Kind, IReadOnlyList<ActionInput> Actions) : ICommand<Guid>;
