# 0005: Authenticated by default, tokens validated with the Auth service's public keys

Status: accepted (2026-10-14)

## Context

Endpoints in the old services had to opt in to `[Authorize]`, and one sensitive endpoint was missed. All services shared a
symmetric signing key, so any service's configuration could mint tokens. Some endpoints took the user id from the URL.

## Decision

- `AddHomeAppsDefaults()` sets a fallback authorization policy that requires an authenticated user. Anonymous endpoints
  must call `.AllowAnonymous()` (only health checks and OpenAPI do in the kit).
- Services validate JWTs with the Auth service's public keys, discovered from `{Jwt:Authority}/.well-known/openid-configuration`.
  Only Auth holds a private key (asymmetric, RS256 or ES256, decided in Phase 2).
- Inbound claim mapping is off; claims keep their JWT names (`sub`, `email`, `role`, `jti`).
- Use cases read the caller from `ICurrentUser` (built from the token), never from the request.

## Consequences

- A leaked service configuration cannot mint tokens.
- Integration tests swap the key for a test key (`HomeAppsApiFactory`, `TestJwt`).
- Until the Auth service exists, running a service locally needs a token issuer at `Jwt:Authority` (the integration tests don't).
