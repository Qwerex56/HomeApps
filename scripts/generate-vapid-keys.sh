#!/usr/bin/env bash
# Prints a new VAPID key pair for the Notifications service (docs/notifications.md), ready to paste into .env.
# Run once per installation and keep the keys: new keys mean every device has to turn notifications on again.
set -euo pipefail

base64url() { openssl base64 -A | tr '+/' '-_' | tr -d '='; }

pem=$(openssl ecparam -name prime256v1 -genkey -noout)
# SEC1 DER: 30 77 02 01 01 04 20 <32-byte private key> ...; the public key DER ends with the 65-byte point 04|X|Y.
private=$(openssl ec -outform DER 2>/dev/null <<<"$pem" | tail -c +8 | head -c 32 | base64url)
public=$(openssl ec -pubout -outform DER 2>/dev/null <<<"$pem" | tail -c 65 | base64url)

echo "NOTIFICATIONS_VAPID_PUBLIC_KEY=$public"
echo "NOTIFICATIONS_VAPID_PRIVATE_KEY=$private"
