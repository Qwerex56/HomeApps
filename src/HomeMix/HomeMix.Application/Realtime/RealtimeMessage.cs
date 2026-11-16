namespace HomeMix.Application.Realtime;

/// <summary>
/// One event for the browsers on the hub (docs/homemix.md, "RealTime"): its group (<see cref="RealtimeGroups"/>), its
/// name as the bridge knows it (<c>data.changed</c>, <c>installation.changed</c>, <c>notification</c>,
/// <c>permissions.changed</c>) and its data, serialized as JSON.
/// </summary>
public sealed record RealtimeMessage(string Group, string Event, object Data);
