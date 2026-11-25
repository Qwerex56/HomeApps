using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Households;

/// <summary>H5, D7: "delete household" for the Owner, archive for a server admin. Its invites are revoked (Events).</summary>
public sealed record ArchiveHouseholdCommand(Guid HouseholdId) : ICommand;
