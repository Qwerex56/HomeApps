---
name: dotnet-conventions
description: HomeApps .NET conventions (Clean Architecture per service, Minimal API endpoints, MediatR 12 commands/queries with FluentValidation and FluentResults, EF Core repositories, AppError). Use when writing, changing or reviewing any C# in this repository.
---

# HomeApps .NET conventions

Stack: .NET 10 (`global.json`), C# latest, nullable on, **warnings are errors** with `AnalysisLevel latest-recommended`.
Central package versions in `Directory.Packages.props` (a `Version` on a `PackageReference` is an error).
MediatR **12.5** and MassTransit **8.5** (free versions; never upgrade), FluentResults 4, FluentValidation 12,
EF Core 10 + Npgsql, MassTransit outbox/inbox over RabbitMQ, `ModelContextProtocol.AspNetCore` for JARVIS tools.

Full recipe: `docs/how-a-service-is-built.md`. Reasons: `docs/adr/0001`–`0009`. Copy the newest service, **Devices**.

## 1. Layers (one service = four projects)

| Project | Holds | References |
|---|---|---|
| `X.Domain` | aggregates, value objects, domain events, `XErrors` | kit Domain, FluentResults |
| `X.Application` | `Abstractions/` (ports), `Common/` (views, helpers), `<Area>/<UseCase>/` slices, `Events/` | Domain, kit Application, `HomeApps.Contracts` |
| `X.Infrastructure` | DbContext, `Persistence/Configurations`, repositories, migrations, MassTransit consumers, jobs | Application, kit Infrastructure |
| `X.Api` | `Program.cs`, `Endpoints/*Endpoints.cs` (`IEndpoint`), `Requests/*Request.cs`, appsettings | Infrastructure, kit Api |

An architecture test fails the build when a layer references the wrong way.

## 2. One type per file, one folder per use case

Every top-level type has its own file named after it (no `Enums.cs`, no `Views.cs` with five records). Only nested
types stay in their parent. A use case is a folder; the namespace equals the folder:

```
Devices.Application/HouseholdDevices/
  AddHouseholdDevice/
    AddHouseholdDeviceCommand.cs     public sealed record ... : ICommand<PairingCodeView>
    AddHouseholdDeviceValidator.cs   internal sealed class ... : AbstractValidator<AddHouseholdDeviceCommand>
    AddHouseholdDeviceHandler.cs     internal sealed class ... : ICommandHandler<AddHouseholdDeviceCommand, PairingCodeView>
  GetHouseholdDevices/
    GetHouseholdDevicesQuery.cs      (no validator when there is nothing to validate)
    GetHouseholdDevicesHandler.cs
  PairingCodeView.cs                 a view shared by the area's use cases
```

`Households.Application/Households/Households.cs` and similar bundled files are legacy: don't copy them, don't
rewrite them unless the spec asks. Never define the same type twice; after a move, delete leftovers.

## 3. Command / query

The record is the input and carries the XML summary with the use case and its rules (cite the spec section).

```csharp
/// <summary>A household admin adds a satellite and gets a one-time code (10 minutes). Names are unique per household.</summary>
public sealed record AddHouseholdDeviceCommand(Guid HouseholdId, string Name) : ICommand<PairingCodeView>;

public sealed record GetHouseholdDevicesQuery(Guid HouseholdId) : IQuery<IReadOnlyList<DeviceView>>;
```

Kit types (`HomeApps.BuildingBlocks.Application.Messaging`): `ICommand`, `ICommand<T>`, `IQuery<T>`,
`ICommandHandler<TCommand>`, `ICommandHandler<TCommand,T>`, `IQueryHandler<TQuery,T>`; all return `Result`/`Result<T>`.

## 4. Validator: shape only

```csharp
internal sealed class AddHouseholdDeviceValidator : AbstractValidator<AddHouseholdDeviceCommand>
{
    public AddHouseholdDeviceValidator()
    {
        RuleFor(c => c.HouseholdId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(Device.MaxNameLength);
    }
}
```

Required, length, format. Business rules (uniqueness, permissions, state) live in the aggregate or handler.
`ValidationBehavior` turns failures into field errors → 400 ValidationProblem. Validators are found automatically.

## 5. Handler: load, check access, call one domain method, return

```csharp
internal sealed class AddHouseholdDeviceHandler(
    IDeviceRepository devices, IHouseholdRoles households, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<AddHouseholdDeviceCommand, PairingCodeView>
{
    public async Task<Result<PairingCodeView>> Handle(AddHouseholdDeviceCommand command, CancellationToken cancellationToken)
    {
        switch (await households.GetRoleAsync(command.HouseholdId, cancellationToken))
        {
            case null: return Result.Fail(DeviceErrors.NotFound);          // outsiders learn nothing: 404, not 403
            case < HouseholdRole.Admin: return Result.Fail(DeviceErrors.Forbidden);
        }
        var added = Device.AddToHousehold(command.HouseholdId, currentUser.RequiredUserId, command.Name, ..., time.GetUtcNow());
        if (added.IsFailed) return added.ToResult();
        devices.Add(added.Value);
        return new PairingCodeView(...);
    }
}
```

- **Never call `SaveChangesAsync`**: `TransactionBehavior` saves after a successful command, nothing on failure.
- **Never throw for business rules**: return `Result.Fail(XErrors.Something)`.
- Identity only from `ICurrentUser.RequiredUserId` (the token), never from the URL or body.
- Time only from `TimeProvider` (tests use `FakeTimeProvider`). Pass `CancellationToken` all the way down.
- A lost optimistic-concurrency race throws `ConcurrencyConflictException` → 409 `concurrency.conflict`, unless the
  handler expects it and returns a typed error.

## 6. Domain: rich model, no base classes

```csharp
public sealed partial class Device
{
    public const int MaxNameLength = 20;
    private readonly List<DeviceAction> _actions = [];
    private Device() { }                                   // for EF
    public Guid Id { get; private set; }                   // declared by the aggregate itself
    public IReadOnlyList<DeviceAction> Actions => _actions;
    public static Result<Device> Create(Guid ownerId, string name, DeviceKind kind, DateTimeOffset now) { ... }
}
```

Private setters, factory methods and behaviour methods returning `Result`. An aggregate that raises events implements
`IHasDomainEvents` (keeps its own list, `ClearDomainEvents()`). Errors are static properties in one `XErrors` class:

```csharp
public static Error NameTaken => new AppError("devices.name_taken", "The household already has a device with this name.", ErrorKind.Conflict);
```

`ErrorKind` picks the status: Validation 400, Unauthorized 401, Forbidden 403, NotFound 404, Conflict 409, Locked 423.
The `code` (`<service>.<snake_case>`) is an API contract: the front end maps it to messages.

## 7. Endpoints: Minimal APIs, discovered automatically

```csharp
internal sealed class HouseholdDevicesEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var household = app.MapGroup("/devices/households/{householdId:guid}").WithTags("Devices");

        household.MapGet("/", async (Guid householdId, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetHouseholdDevicesQuery(householdId), ct)).ToHttpResult())
            .Produces<IReadOnlyList<DeviceView>>();

        household.MapPost("/", async (Guid householdId, AddHouseholdDeviceRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new AddHouseholdDeviceCommand(householdId, body.Name), ct)).ToHttpResult())
            .Produces<PairingCodeView>();
    }
}
```

- End every handler with `.ToHttpResult()` (200/204) or `.ToCreatedHttpResult(v => $"/x/{v.Id}")` (201).
- Every endpoint requires a token unless it calls `.AllowAnonymous()` (rare, explain why in a comment).
- Add `.Produces<T>()` so `npm run api:types` can generate front-end types.
- Request bodies are records in `Api/Requests/<Name>Request.cs`, one per file.
- Rate limits on changing endpoints where the service has them (`.RequireRateLimiting(RateLimits.Changes)`).
- Route prefixes are owned per service by Traefik (`/devices`, `/households`, `/auth` …): stay under your service's prefix.

## 8. Persistence (EF Core)

- One repository per aggregate: interface in `Application/Abstractions/I<Aggregate>Repository.cs`, `internal sealed`
  implementation in `Infrastructure/Persistence/`, registered in `Infrastructure/DependencyInjection.cs`. No generic
  `IRepository<T>`. The DbContext (derives `HomeAppsDbContext`) is the `IUnitOfWork`.
- Repositories expose only what use cases need and return tracked aggregates (the command changes them):

```csharp
public Task<Device?> FindInHouseholdAsync(Guid householdId, string name, CancellationToken cancellationToken) =>
    db.Devices.FirstOrDefaultAsync(d => d.HouseholdId == householdId && d.Name == name, cancellationToken);
```

- Read-only projections (directories, lists for other services, admin search) use `AsNoTracking()` and project to
  what is needed (see `Households.Infrastructure/Persistence/HouseholdDirectory.cs`). Filter and page in SQL, not in memory.
- Mappings are `IEntityTypeConfiguration<T>` in `Persistence/Configurations/` (no attributes on domain classes);
  value objects via conversions or `OwnsMany`; private collections via `UsePropertyAccessMode(PropertyAccessMode.Field)`.
- Each service has its own schema and database. New migration:
  `dotnet tool restore && dotnet ef migrations add <Name> -p src/X/X.Infrastructure -s src/X/X.Api -o Persistence/Migrations`.
  Never edit an applied migration; add a new one.

## 9. Messaging between services

- Contracts: immutable records implementing `IIntegrationEvent` in `src/Contracts/HomeApps.Contracts/<PublishingService>/`,
  one per file. Never a breaking change: add optional fields or a new message.
- Publish from a domain-event handler through `IEventBus` (outbox, same transaction).
- Consumers live in Infrastructure, are `public`, and only translate the message into a command and `Send` it.
- Synchronous calls between services go over the Docker network (`http://auth:8080`), behind a port in Application.

## 10. Auth model (don't confuse the two role systems)

- **Server role** (User < Admin < Owner) is owned by Auth and only governs accounts/server admin.
- **Household role** (Guest < Member < Admin < Owner) lives in Households; other services ask for it through a port
  (`IHouseholdRoles`). Outside a household → 404; inside without the role → 403.
- Deleting people = disable/soft delete; hard erase only via the `UserErased` flow.
- **First-party vs connected-app JWTs**: `TokenIssuer.IssueAccessToken` always emits `client_id` on first-party tokens; distinguish first-party vs connected-app tokens by `aud == JwtOptions.Audience` (never by absence of `client_id`), and cover both with `TestJwt.CreateTokenWithClient`.

## Before you finish

1. `grep` your new files for a second top-level type; split it.
2. No type name defined twice; no unused `using`s.
3. `dotnet build HomeApps.slnx` passes (warnings are errors).
4. New service? Follow the checklist in `docs/how-a-service-is-built.md` and copy Devices (compose, Traefik routes,
   launchSettings port, Dockerfile, docs/<service>.md, CHANGELOG "Unreleased").
