using Auth.Application.Abstractions;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using Microsoft.Extensions.Options;

namespace Auth.Application.ConnectedApps.PreviewConsent;

internal sealed class PreviewConsentHandler(IAppClientRepository clients, IOptions<ConnectedAppsOptions> options)
    : IQueryHandler<PreviewConsentQuery, ConsentPreview>
{
    public async Task<Result<ConsentPreview>> Handle(PreviewConsentQuery query, CancellationToken cancellationToken)
    {
        var checkedRequest = await clients.CheckAsync(query.Request, options.Value, cancellationToken);
        if (checkedRequest.IsFailed)
        {
            return checkedRequest.ToResult<ConsentPreview>();
        }

        var request = checkedRequest.Value;
        return new ConsentPreview(request.Client.Id, request.Client.Name, new Uri(request.RedirectUri).Host, request.Audience);
    }
}
