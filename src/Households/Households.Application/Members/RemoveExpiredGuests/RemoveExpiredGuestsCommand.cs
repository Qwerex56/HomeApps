using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Members;

/// <summary>D2c: run hourly by Infrastructure; removes every guest whose access ended. Returns how many.</summary>
public sealed record RemoveExpiredGuestsCommand : ICommand<int>;
