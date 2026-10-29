namespace Households.Api.Http;

/// <summary>Body of a 201 or of an accepted invite: the household the caller now belongs to.</summary>
internal sealed record IdResponse(Guid Id);

/// <summary>The new guest link code, as people see it (<c>ABCDE-FGHJK</c>).</summary>
internal sealed record GuestLinkResponse(string Code);
