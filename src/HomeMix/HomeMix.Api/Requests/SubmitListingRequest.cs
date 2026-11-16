namespace HomeMix.Api.Requests;

/// <summary>A GitHub repository link, as a person pastes it.</summary>
public sealed record SubmitListingRequest(string Repository);
