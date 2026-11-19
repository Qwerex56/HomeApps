---
name: testing-standards
description: Where and how to write HomeApps tests (.NET xUnit domain/application/API tests on Testcontainers Postgres, Vitest + Vue Test Utils for src/Web, Playwright smoke) and the exact commands to run them. Use when adding tests, running them or reading their results.
---

# HomeApps testing standards

Tests prove the **spec's acceptance criteria** (Given/When/Then) and the **ADR's error codes**, not the
implementation. One criterion → at least one test whose name says the behaviour in plain words.

## .NET: layout

Packages (central versions): xUnit 2.9, NSubstitute 5, `Microsoft.AspNetCore.Mvc.Testing`, Testcontainers
(PostgreSql, RabbitMq), MassTransit test harness. `tests/Directory.Build.props` adds xUnit and `using Xunit;`,
and allows underscores in test names.

| Project | What | Needs |
|---|---|---|
| `tests/<Svc>/<Svc>.Domain.Tests` | aggregates and value objects only, no mocks, no DI | nothing |
| `tests/<Svc>/<Svc>.Application.Tests` | handlers through the real MediatR pipeline (validation + transaction) with in-memory ports (`World.cs` + `Fakes.cs` in Households) | nothing |
| `tests/<Svc>/<Svc>.Infrastructure.Tests` | EF mappings, repositories, consumers against real Postgres | Docker |
| `tests/<Svc>/<Svc>.Api.Tests` | the real API over HTTP, real Postgres, in-memory MassTransit harness | Docker |
| `tests/Integration/HomeApps.IntegrationTests` | **anything that spans services** (owner's rule) | Docker |
| `tests/BuildingBlocks/*` | kit changes always come with a test here | Docker for API parts |

A new service gets at least `Domain.Tests` and `Api.Tests` (copy `tests/Devices`).

## .NET: patterns from the repo

Domain test: fixed time, a `Theory` for each rule, assert the **error code**.

```csharp
public sealed class DeviceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 1, 0, 0, TimeSpan.FromHours(1));

    [Theory]
    [InlineData("")]
    [InlineData("Mac")]
    [InlineData("a-name-longer-than-twenty")]
    public void Other_names_are_refused(string name) =>
        Assert.Equal("devices.invalid_name", Code(Device.Create(Guid.NewGuid(), name, DeviceKind.Computer, Now).Errors));
}
```

Application test: a `World` wires the real `Add<Svc>Application()` with in-memory ports, `SignInAs(user)` sets
`ICurrentUser`, `Send` goes through the pipeline. Prefix the test with the spec's use-case id when it has one.

```csharp
[Fact]
public async Task D2a_GuestMayNotSeeTheListAndAStrangerDoesNotKnowIt()
{
    using var w = new World();
    var f = await w.Family();
    w.SignInAs(f.Guest);
    Assert.Equal("household.forbidden", (await w.Send(new ListMembersQuery(f.Id))).Code());
    w.SignInAs(Guid.NewGuid());
    Assert.Equal("household.not_found", (await w.Send(new ListMembersQuery(f.Id))).Code());
}
```

API test: a factory derived from `HomeAppsApiFactory<Program, TDbContext>`
(`tests/BuildingBlocks/HomeApps.BuildingBlocks.Testing`) replaces cross-service ports with fakes; every test uses
fresh `Guid`s so tests share the database safely.

```csharp
public sealed class DevicesApiFactory : HomeAppsApiFactory<Program, DevicesDbContext>
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.RemoveAll<IHouseholdRoles>();
        services.AddScoped<IHouseholdRoles, FakeHouseholdRoles>();
    }
}

public sealed class HouseholdDevicesTests(DevicesApiFactory api) : IClassFixture<DevicesApiFactory>
{
    [Fact]
    public async Task Only_admins_change_household_devices_and_outsiders_learn_nothing()
    {
        using var member = api.CreateAuthenticatedClient(_member);
        using var outsider = api.CreateAuthenticatedClient(Guid.NewGuid());
        Assert.Equal(HttpStatusCode.Forbidden, (await AddAsync(member, "lazienka")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await AddAsync(outsider, "garaz")).StatusCode);
    }
}
```

- `CreateAuthenticatedClient(userId, email?, roles…)` sends a valid token; `CreateAssistantClient` a JARVIS token.
- Published/consumed messages: `await api.Harness.Published.Any<T>(…)` / `Consumed.Any<T>(…)`.
- Time: a `FixedTime(now) : TimeProvider` (one per test project, its own file) or `FakeTimeProvider`.
- One fake/builder/fixture per file (one type per file applies to tests too).

## .NET: what every feature needs

1. Domain: each rule and each `AppError` the aggregate can return.
2. Application or API: the happy path per acceptance criterion; **permissions** (outsider → 404, too low a role → 403,
   anonymous → 401); validation → 400 with the field name; conflicts → 409 with the `code`.
3. Messages: a published contract when the spec says other services react; a consumer translating into the command.
4. Edge cases from the spec's section 4 and the ADR's "Ryzyka" list, each as its own test.

## .NET: commands

```bash
dotnet build HomeApps.slnx                                           # warnings are errors
dotnet test tests/Devices/Devices.Domain.Tests                       # one project, no Docker
dotnet test tests/Devices/Devices.Api.Tests                          # needs Docker running
dotnet test tests/Devices/Devices.Api.Tests --filter "FullyQualifiedName~HouseholdDevicesTests"
dotnet test HomeApps.slnx                                            # everything, needs Docker (CI runs this in Release)
dotnet test HomeApps.slnx 2>&1 | grep -E "error|Failed|Passed!|Failed!" | tail -40
```

There is no `Category` trait in use: to skip Docker, run only `*.Domain.Tests` and `*.Application.Tests` projects.
If Docker is not running, say so in the report (`Api.Tests` will fail on container start); don't mark them passed.

## Front end (src/Web)

Vitest 5 (jsdom) + `@vue/test-utils`; specs in `__tests__/` next to the code (`features/devices/__tests__/*.spec.ts`).

```ts
function render(role: 'Member' | 'Admin') {
  return mount(HouseholdDevicesTab, {
    props: { household: { id: 'h1', name: 'Dom', myRole: role } as Household, can: householdPermissions(role) },
    global: {
      plugins: [i18n, PrimeVue, ToastService, ConfirmationService,
        [VueQueryPlugin, { queryClient: new QueryClient({ defaultOptions: { queries: { retry: false } } }) }]],
    },
    attachTo: document.body,
  })
}

afterEach(() => { vi.restoreAllMocks(); document.body.innerHTML = '' })

it('shows a member the satellites without anything to change', async () => {
  vi.spyOn(devicesApi, 'household').mockResolvedValue([salon])
  const wrapper = render('Member')
  await flushPromises()
  expect(wrapper.text()).toContain('salon')
  expect(wrapper.text()).not.toContain('Dodaj satelitę')
})
```

- Mock the feature's `xxxApi` object with `vi.spyOn`, never axios internals. `api.spec.ts` checks URLs/payloads.
- Assert on Polish text the user sees (from `pl.json`), per role, and for the error, loading and empty states.
- A server error: `mockRejectedValue` with an Axios-like error carrying a ProblemDetails, then expect the translated message.

```bash
cd src/Web
npm run test:run                                   # all unit tests once
npx vitest run src/features/devices                # one feature
npm run type-check                                 # vue-tsc
npx eslint .                                       # check only (npm run lint also fixes)
npm run build                                      # type-check + vite build
E2E_BASE_URL=https://app.localhost npm run test:e2e   # Playwright smoke against a RUNNING stack (docs/web.md)
```

E2E is a smoke test of a running stack; don't add feature e2e tests unless the spec asks.

## Report format (tester → orchestrator)

```
VERDICT: PASS | FAIL
Backend:  <project>: <passed>/<total> (… per project)
Frontend: vitest <passed>/<total>; type-check OK|FAIL; eslint OK|FAIL
Not run:  <what and why, e.g. Docker not running>
Criteria covered: <spec criterion → test name>, …
Failures (FAIL only): <test name> — <first 20 lines of the error>
```

Never skip, disable or delete a test to get green; never weaken an assertion that encodes the spec.
