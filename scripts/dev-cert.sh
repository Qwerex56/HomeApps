#!/usr/bin/env sh
# The development certificate for https://app.localhost and *.app.localhost, signed by mkcert's local CA, which
# `mkcert -install` makes trusted on this machine: browsers, the .NET voice bridge (no --insecure) and curl.
# Once per machine, then restart Traefik (docs/development.md).
set -eu
cd "$(dirname "$0")/.."
command -v mkcert >/dev/null 2>&1 || { echo "mkcert is missing: brew install mkcert nss" >&2; exit 1; }
mkcert -install
mkdir -p certs
mkcert -cert-file certs/app.localhost.pem -key-file certs/app.localhost-key.pem app.localhost '*.app.localhost'
echo "Done. If Traefik is running: docker compose -f compose.dev.yaml restart traefik"
