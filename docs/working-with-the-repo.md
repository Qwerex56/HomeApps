# Working with the repository

How to build, test and change HomeApps day to day. For how a new service is put together, see
[how-a-service-is-built.md](how-a-service-is-built.md).

## What you need

- .NET 10 SDK (`global.json` accepts 10.0 or newer).
- Docker, for the local infrastructure (`compose.dev.yaml`), for tests that need Postgres, RabbitMQ or Mailpit
  (Testcontainers) and for building images. See [auth.md](auth.md) for running Auth locally.

## What is where

| Path | What it is |
| --- | --- |
| `src/BuildingBlocks/` | The shared kit: `HomeApps.BuildingBlocks.Domain`, `.Application`, `.Infrastructure`, `.Api`. |
| `src/Contracts/` | `HomeApps.Contracts`: only the messages and names that cross service boundaries. |
| `src/<Service>/`, `tests/<Service>/` | One folder per service (Auth, Accounts, Household), written on the kit. |
| `tests/BuildingBlocks/` | Kit tests and the shared integration-test fixture. |
| `compose.dev.yaml` | Local infrastructure: Traefik, Postgres, RabbitMQ, Mailpit; services in the `services` profile. |
| `.env.example` | What compose reads; copy to `.env` (never committed) to change it. |
| `HomeApps.slnx` | The one solution file, with a folder for BuildingBlocks, Contracts and each service. Open it in Rider; CI builds it. |

No C# project lives in the repository root: all code is in `src/` and `tests/`.
| `Directory.Build.props` | Settings for every project (see below). |
| `Directory.Packages.props` | The one list of package versions (see below). |

## Build and test

From the repository root:

```bash
dotnet build HomeApps.slnx
dotnet test HomeApps.slnx
```

This is exactly what CI runs.

## Project settings: Directory.Build.props

The root `Directory.Build.props` applies to every project: `net10.0`, nullable reference types, implicit
usings and warnings as errors. A new project's csproj does not need to repeat any of that.

- `src/Directory.Build.props` adds analyzers for the kit and the services, and `tests/Directory.Build.props`
  adds xUnit and the test SDK to test projects. Both build on the root file.

## Packages: Directory.Packages.props

Package versions live in one place, the root `Directory.Packages.props` (central package management). A
project names the package and never its version.

To add a package:

1. If the package is not in `Directory.Packages.props` yet, add it there under the fitting label:
   `<PackageVersion Include="Some.Package" Version="1.2.3" />`.
2. Reference it from the project without a version: `<PackageReference Include="Some.Package" />`.

`dotnet add package Some.Package` in a project folder does both steps for you when central package management is on.

To bump a package, change its version in `Directory.Packages.props`. Every project that uses it moves together,
so build and test the whole solution afterwards. A `Version` attribute on a `PackageReference` is an error
(NU1008); if one project really needs a different version for a while, use
`<PackageReference Include="X" VersionOverride="1.2.3" />` and leave a comment why.

Docker builds copy both root props files before `dotnet restore`; a new Dockerfile needs the same line:

```dockerfile
COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
```

## Branches, pull requests and CI

1. Update master: `git checkout master && git pull`.
2. Create a branch for the issue: `git checkout -b 60-short-description`.
3. Make small commits, one change each, and mention the issue (`Refs #60`).
4. Build and test locally (see above), then push: `git push -u origin 60-short-description`.
5. Open a pull request into `master`. Write `Closes #60` in the description to close the issue on merge.
6. Merge once CI is green.

What runs on GitHub:

| Workflow | When | What |
| --- | --- | --- |
| `ci.yml` | Every pull request, and pushes to master | Restores, builds and tests `HomeApps.slnx` in Release. |

If CI fails, open the failed run from the pull request's Checks tab; the step log shows the compiler or test
error. Fix it on the same branch and push again; CI reruns by itself.
