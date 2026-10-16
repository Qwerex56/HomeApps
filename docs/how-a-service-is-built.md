# How a service is built

Every HomeApps service has the same shape, built on the shared kit (`src/BuildingBlocks`, `src/Contracts`).
This page is the whole recipe. The reasons behind it are in [the ADRs](adr/). There is no generator: the first
service (Auth) is the reference to copy from, and Claude writes the repetitive parts.

## Projects

A service called `Accounts` has four projects under `src/Accounts` and three test projects under `tests/Accounts`,
all in one solution folder of `HomeApps.slnx`.

| Project | Holds | May reference |
|---|---|---|
| `Accounts.Domain` | Aggregates, entities, value objects (records), domain events | BuildingBlocks.Domain only |
| `Accounts.Application` | Use cases as slices, ports (repository interfaces) | Domain, BuildingBlocks.Application, Contracts |
| `Accounts.Infrastructure` | DbContext, EF configurations, repositories, consumers | Application, BuildingBlocks.Infrastructure |
| `Accounts.Api` | `Program.cs` and endpoints (Minimal APIs, `IEndpoint`) | Infrastructure, BuildingBlocks.Api |

An architecture test in the integration test project fails the build when a layer references the wrong way.

## Add a use case

A use case is one folder, `Application/<Feature>/<UseCase>/`, with the command or query record, its validator
(commands) and its handler, plus one `IEndpoint` class in `Api/<Feature>/`. Endpoints are discovered automatically;
nothing to register.

1. **The command or query record**: the input. Commands implement `ICommand` / `ICommand<T>`, queries `IQuery<T>`.
2. **The validator**: shape only (required, length, format). Business rules go in the aggregate.
3. **The handler**: load the aggregate through its repository, call one domain method, return its `Result`.
   Don't call `SaveChangesAsync`: the transaction behavior saves after a successful command and saves nothing on failure.
4. **The endpoint**: map the route, `Send` the request and end with `.ToHttpResult()`.

## The rules, in one place

- **Rich domain, no base classes.** An aggregate is a plain class that declares its own `Id`, keeps setters private
  and exposes methods that return `Result` / `Result<T>`. Don't throw for rules. There are no typed errors yet:
  fail with a message (`Result.Fail("...")`) and add an error type when a service really needs another HTTP status.
- **One mapper to HTTP.** Endpoints end with `.ToHttpResult()` (200/204) or `.ToCreatedHttpResult(...)` (201).
  Every failure becomes a 400: a ValidationProblem when the errors name fields (validation), otherwise a
  ProblemDetails with the first error's message. Unhandled exceptions become a generic 500 that never shows the
  exception message.
- **Identity from the token only.** Inject `ICurrentUser`; never take "me" from the URL or body.
  Every endpoint requires a valid token unless it calls `.AllowAnonymous()`.
- **One repository per aggregate**, declared in `Application/Abstractions`, implemented in Infrastructure, registered in
  `Infrastructure/DependencyInjection.cs`. No generic `IRepository<T>`. The DbContext is the `IUnitOfWork`.
- **Events.** An aggregate that raises domain events implements `IHasDomainEvents`: it keeps its own list, adds
  events to it in its methods and clears it in `ClearDomainEvents()`. When the unit of work saves, they are dispatched in-process to
  `IDomainEventHandler<T>` before the commit. To tell other services, publish a `HomeApps.Contracts` message through
  `IEventBus` from that handler: it goes into the outbox in the same transaction, and MassTransit delivers it after the commit.
- **Consumers** live in Infrastructure, must be `public`, and only translate the message into a command and `Send` it.
  The inbox drops duplicate deliveries.
- **Contracts** are immutable records implementing `IIntegrationEvent`, in a folder named after the publishing service.
  Never make a breaking change to a published message: add optional fields or a new message.
- **Time** comes from the .NET `TimeProvider` (registered by the kit), so tests can use `FakeTimeProvider`.

## Database

Entity mappings are `IEntityTypeConfiguration<T>` classes in Infrastructure (no attributes on domain classes);
they are picked up automatically. Each service uses its own schema (the service name). Create a migration with:

```bash
dotnet tool restore
dotnet ef migrations add Initial -p src/Accounts/Accounts.Infrastructure -s src/Accounts/Accounts.Api -o Persistence/Migrations
```

In Development the API applies migrations on start.

## Configuration

| Key | Meaning |
|---|---|
| `ConnectionStrings:Database` | The service's Postgres database |
| `Jwt:Authority` | Base URL of the Auth service; public keys come from its `/.well-known/openid-configuration` |
| `Jwt:Audience`, `Jwt:Issuer`, `Jwt:RequireHttpsMetadata` | Token validation; `Issuer` defaults to `Authority` |
| `RabbitMq:Host`, `Port`, `VirtualHost`, `Username`, `Password` | Broker connection |
| `Messaging:Transport` | `RabbitMq` (default) or `InMemory` (tests, or running one service alone) |

Health: `GET /health/live` (process) and `GET /health/ready` (database), both anonymous.
OpenAPI: `GET /openapi/v1.json` in Development.

## Tests

- `*.Domain.Tests`: aggregates only, no mocks.
- `*.Application.Tests`: handlers with NSubstitute fakes for the ports.
- `*.IntegrationTests`: architecture tests, plus API tests (`[Trait("Category", "Integration")]`) that run the real API
  against Postgres in Docker with MassTransit's in-memory harness. `HomeAppsApiFactory` (in
  `tests/BuildingBlocks/HomeApps.BuildingBlocks.Testing`) does the setup; `CreateAuthenticatedClient(userId)` sends a valid token.

```bash
dotnet test                                   # everything (needs Docker)
dotnet test --filter "Category!=Integration"  # no Docker
```

## Changing the kit

The kit is shared by every service, so keep it to building blocks and contracts: no service-specific code, and no
abstraction until a service really needs it. Kit changes come with a test in `tests/BuildingBlocks`.
