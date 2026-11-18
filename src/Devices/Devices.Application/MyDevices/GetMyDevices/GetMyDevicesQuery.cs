using Devices.Application.Common;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.MyDevices.GetMyDevices;

/// <summary>My devices with their actions and whether they are connected now; removed ones last.</summary>
public sealed record GetMyDevicesQuery : IQuery<IReadOnlyList<DeviceView>>;
