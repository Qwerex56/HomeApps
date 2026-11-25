using Accounts.Application.Abstractions;
using Accounts.Application.Common;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Accounts.Application.MyAccount;

/// <summary>UC-04, Q2, Q6: when and what kind of change, newest first, at most <see cref="Limit"/>. No authors, no fields.</summary>
public sealed record GetMyHistoryQuery : IQuery<IReadOnlyList<MyHistoryEntryView>>
{
    public const int Limit = 50;
}
