namespace HomeMix.Api.Requests;

/// <summary>The device a widget shows; <c>null</c> goes back to the best match.</summary>
public sealed record ChooseWidgetDeviceRequest(Guid? Device);
