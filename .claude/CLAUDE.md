# HomeApps for Claude Code

HomeApps is a self-hosted smart home for families: .NET 10 microservices (Auth, Accounts, Households, Notifications,
HomeMix, Devices, Jarvis) on a shared kit, a Vue 3 web app (`src/Web`) and the JARVIS assistant on top (MCP tools).
The owner (team lead) designs; Claude writes the code, including all the repetitive parts.

## How work flows here

1. **Spec** (`specs/<name>.md`): written in the Claude Desktop project (Chat), WHAT and acceptance criteria only.
2. **Debate** (`/spec-debate @specs/<name>.md`): architects + devil's advocate produce `specs/<name>-adr.md`.
3. **Build** (`/run-feature @specs/<name>.md`): developer (Sonnet) → tester (Haiku) → reviewer (Opus), at most
   3 review rounds (5 when the local LLM router is up), then docs-writer (Haiku) and a GitHub PR for the owner. Never merge into the base branch yourself.

## Where to look first

- `docs/how-a-service-is-built.md`: the whole backend recipe. `docs/adr/`: decisions for every service.
- `docs/working-with-the-repo.md`: build, test, branches, CI. `docs/web.md`: the front end.
- Newest code to copy from: `src/Devices` (backend), `src/Web/src/features/devices` (front end).
  Older services (Households, parts of Accounts) still bundle several features in one file: don't copy that.

## Commands

| What | Command |
|---|---|
| Build everything (warnings are errors) | `dotnet build HomeApps.slnx` |
| One test project | `dotnet test tests/<Service>/<Project>` |
| All .NET tests (Api tests need Docker: Testcontainers Postgres) | `dotnet test HomeApps.slnx` |
| Front end checks (in `src/Web`) | `npm run type-check`, `npm run test:run`, `npx eslint .`, `npm run build` |

`npm run lint` runs eslint with `--fix`: use `npx eslint .` when you only want to check.

## Standing rules

- Skills: `dotnet-conventions`, `vue-conventions`, `testing-standards`, `code-review-checklist`.
- One type per file; one folder per command/query with `Command`/`Validator`/`Handler` files.
- Lean: no new abstractions, base classes or generic repositories without a real need.
- Never touch `master` directly and never force-push. During a multi-stage plan the owner may name an integration
  branch (e.g. `smart-devices/main`); branch from it and open the PR against it.
- Never skip, disable or delete tests to get green.
- `specs/` is written by the owner and by `/spec-debate`; implementation agents only read it. Exception: the
  `/run-feature` orchestrator marks the finished stage in the spec's section 0 (status table) before the PR.
- Answer the owner in Polish. Code, comments, commits and docs stay in English.
- Keep tokens low: grep and read line ranges, build/test only affected projects, pipe output through `tail`/`grep`.
