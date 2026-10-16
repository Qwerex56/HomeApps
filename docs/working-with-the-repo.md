# Working with the repository

How to build, test and change HomeApps day to day. For how a new service is put together, see
[how-a-service-is-built.md](how-a-service-is-built.md).

## What you need

- .NET 10 SDK (`global.json` accepts 10.0 or newer).
- Docker, only for integration tests (Testcontainers starts Postgres) and for building images.

## What is where

| Path | What it is |
| --- | --- |
| `AccountManagement/`, `Household.Api/`, `AuthorizationService/`, `Shared/` | The current services. They keep running as they are while the new ones are built next to them. |
| `TodoListService/` | Work in progress. It is in the repository but not in the solution yet, because it does not compile. Add it back with `dotnet sln HomeApps.slnx add TodoListService/TodoListService.csproj` once it builds. |
| `src/`, `tests/` | The shared kit (`HomeApps.BuildingBlocks.*`, `HomeApps.Contracts`) and every new service generated from the templates. |
| `templates/` | The `dotnet new` templates for services, commands and queries. |
| `HomeApps.slnx` | The one solution file. Open it in Rider; CI builds it. |
| `Directory.Build.props` | Settings for every project (see below). |
| `Directory.Packages.props` | The one list of package versions (see below). |

## Build and test

From the repository root:

```bash
dotnet build HomeApps.slnx
dotnet test HomeApps.slnx
```

This is exactly what CI runs. To check the templates still produce working services (this is what the
template workflow runs), use `./templates/verify.sh`, or `./templates/verify.sh --no-docker` to skip the
integration tests.

## Project settings: Directory.Build.props

The root `Directory.Build.props` applies to every project: `net10.0`, nullable reference types, implicit
usings and warnings as errors. A new project's csproj does not need to repeat any of that.

- `src/Directory.Build.props` adds analyzers for the shared kit and new services, and `tests/Directory.Build.props`
  adds xUnit and the test SDK to test projects. Both build on the root file.
- The current services are listed in the root file as an exception to warnings as errors, because they still
  have warnings. Remove a service from that list once its warnings are fixed or it is replaced. Never add a new
  project to it.

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
| `templates.yml` | Pull requests touching `src/`, `tests/` or `templates/` | Runs the shared kit tests and `templates/verify.sh`. |
| `docker-image.yml` | Pushes to master | Builds and pushes the AccountManagement image to Docker Hub. |

If CI fails, open the failed run from the pull request's Checks tab; the step log shows the compiler or test
error. Fix it on the same branch and push again; CI reruns by itself.
