# Working with the repository

How to build, test and change HomeApps day to day. Running it locally (compose, Rider, plugins, add-ons, JARVIS) is
[development.md](development.md); how a new service is put together is [how-a-service-is-built.md](how-a-service-is-built.md).

## What you need

- .NET 10 SDK (`global.json` accepts 10.0 or newer).
- Docker, for the local stack (`compose.dev.yaml`, [development.md](development.md)), for tests that need Postgres,
  RabbitMQ or Mailpit (Testcontainers) and for building images.

## What is where

| Path | What it is |
| --- | --- |
| `src/BuildingBlocks/` | The shared kit: `HomeApps.BuildingBlocks.Domain`, `.Application`, `.Infrastructure`, `.Api`, and the plugin kit `HomeApps.Plugins`, `.Hosting` ([plugins.md](plugins.md)). |
| `src/Contracts/` | `HomeApps.Contracts`: only the messages and names that cross service boundaries. |
| `src/<Service>/`, `tests/<Service>/` | One folder per service (Auth, Accounts, Households, Notifications, HomeMix, Devices, Jarvis), written on the kit. |
| `tests/BuildingBlocks/` | Kit tests, the shared integration-test fixture and `HomeApps.Plugins.Hello`, the sample plugin. |
| `compose.dev.yaml`, `dynamic/`, `scripts/dev-cert.sh` | Local development: Traefik, Postgres, RabbitMQ, Mailpit, the services in the `services` profile, routes to Rider and the certificate ([development.md](development.md)). |
| `.env.example` | What the dev compose reads; copy to `.env` (never committed). |
| `compose.voice.yaml` | Speaches, JARVIS's ears and voice ([jarvis.md](jarvis.md)). |
| `HomeApps.slnx` | The one solution file, with a folder for BuildingBlocks, Contracts and each service. Open it in Rider; CI builds it. |
| `Directory.Build.props` | Settings for every project, the version among them (see below). |
| `Directory.Packages.props` | The one list of package versions (see below). |
| `src/Web/` | The Vue app ([web.md](web.md)), with its production image (`Dockerfile`, `nginx.conf`). |
| `compose.prod.yaml`, `.env.prod.example` | Production: everything behind Traefik with Let's Encrypt ([deployment.md](deployment.md)). |
| `compose.smoke.yaml`, `scripts/` | The release check (`smoke.sh`), the signing key generator and `pack.sh`, which packs the kit for plugins. |
| `tools/mac-device-emulator/` | Standalone .NET solution emulating Mac-based test devices (`homeapps.device/1`) for local testing ([Mac device emulator](../tools/mac-device-emulator/README.md)). |
| `CHANGELOG.md` | What each version changed. |

No C# project lives in the repository root: all code is in `src/` and `tests/`.

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
| `homemix-npm.yml` | A tag `homemix-v<version>`, or by hand | Builds the HomeMix tooling, checks the official add-ons with it and publishes `@home-apps/sdk`, `@home-apps/css`, `@home-apps/vue`, `@home-apps/homemix` and `@home-apps/create-homemix` to npm (secret `NPM_TOKEN`). |

If CI fails, open the failed run from the pull request's Checks tab; the step log shows the compiler or test
error. Fix it on the same branch and push again; CI reruns by itself.

## Releases

1. Set `<Version>` in `Directory.Build.props` and the image tags in `compose.prod.yaml` to the new version.
2. Add the version to [CHANGELOG.md](../CHANGELOG.md), including any new setting in `.env.prod.example`.
3. Run both smoke tests on a fresh stack ([deployment.md](deployment.md#checking-a-release-the-smoke-tests)).
4. Tag master: `git tag -a v2.0.0 -m "HomeApps 2.0.0"`.
5. If the HomeMix tooling changed, set the same version in the five `src/Web/homemix/*/package.json` (and the
   `@home-apps/sdk` dependency of `vue` and `cli`, `@home-apps/css` of `vue`), then push a tag `homemix-v2.4.0`: `homemix-npm.yml` publishes them.
   Authors get them with `npm create @home-apps/homemix@latest` ([homemix-authoring.md](homemix-authoring.md)).

### Publishing to npm the first time

1. Create an npm account with two-factor authentication, and an organization named `home-apps` (free for public
   packages), so the `@home-apps` scope is yours. All five packages live in it; the workflow publishes them with
   `--access public`.
2. Create a granular access token that may read and write packages (all of yours, or the five by name; until the
   first publish, the whole `@home-apps` scope), and add it to the repository as the secret `NPM_TOKEN`
   (Settings → Secrets and variables → Actions).
3. The repository must be public: `--provenance` links each package to the commit and workflow that built it.
4. Push the tag: `git tag homemix-v2.4.0 && git push origin homemix-v2.4.0`, and watch the run in Actions.
5. Check `npm view @home-apps/sdk` and try `npm create @home-apps/homemix@latest test-addon` in an empty folder.
