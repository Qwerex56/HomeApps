# Changelog

Versions follow [Semantic Versioning](https://semver.org). Tags are `v<version>`.

## 2.1.0 (unreleased)

### Added

- **Plugins:** the kit for apps that live outside this repository ([docs/plugins.md](docs/plugins.md)). The plugin
  manifest (`GET /plugins/{id}/manifest`), checked at start; household roles asked from Households with a short cache
  that Households' events clear; permissions per household role checked before every plugin command and query;
  switching a plugin on per household; everything under `/plugins/{id}`; the built UI served at `/plugins/{id}/ui`.
- `scripts/pack.sh` packs the kit into a local NuGet feed for plugins.

## 2.0.0 (2026-10-29)

A rewrite from scratch. The services of the first version (`AccountManagement`, `Household.Api`, `TodoListService`)
and the `release/1.x` branches are history only; nothing of their data or API carries over.

### Added

- **Auth:** sign-in with a password or Google, RS256 access tokens with JWKS, refresh tokens with rotation, theft
  detection and a 10-second grace window, sessions per device, invitations (the first Owner from a bootstrap
  invitation), password reset, email change, server roles User, Admin and Owner, rate limits.
- **Accounts:** profiles created from Auth's events, account history, disabling and reactivation, administration,
  erasing on request.
- **Households:** households with Owner, Admin, Member and Guest roles, invites by code, a guest link, ownership
  transfer (automatic when the Owner is banned or erased), archiving, a member limit.
- **Web:** the Vue 3 app for all of the above, installable as a PWA, with a wall tablet mode.
- **Running it:** `compose.prod.yaml` with Traefik and Let's Encrypt, the signing key as a Docker secret, migrations
  on start, `scripts/smoke.sh` and a Playwright browser test for checking a release ([docs/deployment.md](docs/deployment.md)).
