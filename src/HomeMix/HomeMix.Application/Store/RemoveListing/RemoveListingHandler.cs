using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Store;

namespace HomeMix.Application.Store.RemoveListing;

internal sealed class RemoveListingHandler(IStoreListingRepository listings, ICurrentUser currentUser) : ICommandHandler<RemoveListingCommand>
{
    public async Task<Result> Handle(RemoveListingCommand command, CancellationToken cancellationToken)
    {
        var found = await StoreEntries.FindForAuthorAsync(
            command.Key, listings, currentUser.RequiredUserId, currentUser.IsInRole("Owner"), cancellationToken);
        if (found.IsFailed)
        {
            return found.ToResult();
        }

        // An official entry comes back from addons.json on the next refresh; it leaves by a pull request there.
        if (found.Value.Official)
        {
            return Result.Fail(StoreErrors.Official);
        }

        found.Value.Remove();
        return Result.Ok();
    }
}
