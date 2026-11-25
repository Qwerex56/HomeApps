using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Households;

/// <summary>H2: my households with my role (Q7: no paging). Guests do not see the member count (D2a).</summary>
public sealed record GetMyHouseholdsQuery : IQuery<IReadOnlyList<MyHouseholdView>>;
