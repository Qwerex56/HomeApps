#!/usr/bin/env bash
# Creates secrets/auth-signing.pem, the RSA key Auth signs access tokens with (compose.prod.yaml mounts it).
# Run once per installation. A new key signs everyone out within 15 minutes; see docs/deployment.md for a rotation
# that keeps sessions (Auth:Signing:PreviousPublicKeyPem).
set -euo pipefail

cd "$(dirname "$0")/.."
key=secrets/auth-signing.pem

if [[ -e "$key" && "${1:-}" != "--force" ]]; then
    echo "$key already exists; pass --force to replace it." >&2
    exit 1
fi

mkdir -p secrets
openssl genpkey -quiet -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out "$key"
# The auth container runs as a non-root user and must be able to read the mounted file.
chmod 644 "$key"
echo "Created $key"
