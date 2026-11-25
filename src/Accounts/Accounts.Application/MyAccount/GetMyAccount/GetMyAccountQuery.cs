using Accounts.Application.Abstractions;
using Accounts.Application.Common;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Accounts.Application.MyAccount;

/// <summary>UC-01: my account. Missing right after sign-up while Auth's event is on its way: 404 <c>user.pending</c>, retry.</summary>
public sealed record GetMyAccountQuery : IQuery<MyAccountView>;
