#!/usr/bin/env bash
# Smoke test of the whole release on a running stack with Mailpit (release plan E3):
#   HOMEAPPS_DOMAIN=home-apps.localhost docker compose -f compose.prod.yaml -f compose.smoke.yaml up -d --build
#   scripts/smoke.sh
# Needs an EMPTY database (the first Owner comes from the bootstrap invitation). Exits non-zero on the first failure.
# Settings: HOMEAPPS_DOMAIN (home-apps.localhost), OWNER_EMAIL (owner@example.com, the AUTH_BOOTSTRAP_EMAIL of the
# stack), MAILPIT (http://localhost:8025), TRAEFIK_IP (127.0.0.1).
set -euo pipefail

domain=${HOMEAPPS_DOMAIN:-home-apps.localhost}
owner_email=${OWNER_EMAIL:-owner@example.com}
mailpit=${MAILPIT:-http://localhost:8025}
base="https://$domain"
member_email="member-$RANDOM@example.com"
password="Smoke test password 1"
client=homeapps-web
work=$(mktemp -d)
trap 'rm -rf "$work"' EXIT

# -k: the smoke overlay has no Let's Encrypt; --resolve: the domain need not exist in DNS.
curl_base=(curl -sS -k --noproxy '*' --resolve "$domain:443:${TRAEFIK_IP:-127.0.0.1}" -H "Origin: $base")

step() { printf '\n== %s\n' "$*"; }
fail() { printf 'FAIL: %s\n' "$*" >&2; exit 1; }

# call <jar> <method> <path> [json] [token]: prints the body, sets $status. In a pipeline (call ... | jq) $status is
# set only in the pipeline's subshell, so the status also goes to a file that expect reads.
call() {
    local jar=$1 method=$2 path=$3 body=${4:-} token=${5:-}
    local args=("${curl_base[@]}" -b "$work/$jar" -c "$work/$jar" -o "$work/body" -w '%{http_code}' -X "$method")
    [[ -n $body ]] && args+=(-H 'Content-Type: application/json' --data "$body")
    [[ -n $token ]] && args+=(-H "Authorization: Bearer $token")
    status=$("${args[@]}" "$base$path")
    echo "$status" > "$work/status"
    cat "$work/body"
}

# expect <status> <what>; "2xx" accepts any success.
expect() { status=$(cat "$work/status"); [[ $status == "$1" || ( $1 == 2xx && $status == 2* ) ]] || fail "$2: expected $1, got $status: $(cat "$work/body")"; echo "ok  $2 ($status)"; }

# waits for an email to <address> and prints the first link to <base>/<route>/...
mail_link() {
    local address=$1 route=$2 id
    for _ in $(seq 1 30); do
        id=$(curl -sS --noproxy '*' "$mailpit/api/v1/search?query=to:%22$address%22" | jq -r '.messages[0].ID // empty')
        if [[ -n $id ]]; then
            curl -sS --noproxy '*' "$mailpit/api/v1/message/$id" | jq -r '.Text' \
                | grep -oE "$base/$route/[A-Za-z0-9_-]+" | head -n 1
            return
        fi
        sleep 1
    done
    fail "no email to $address"
}

# mcp <method> <params> [token]: one JSON-RPC call to the JARVIS gateway (stateless, so no initialize); prints the
# result, from plain JSON or from the "data:" line of an event stream.
mcp() {
    local args=("${curl_base[@]}" -o "$work/body" -w '%{http_code}' -X POST -H 'Content-Type: application/json'
        -H 'Accept: application/json, text/event-stream'
        --data "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"$1\",\"params\":$2}")
    [[ -n ${3:-} ]] && args+=(-H "Authorization: Bearer $3")
    status=$("${args[@]}" "$base/jarvis/mcp")
    echo "$status" > "$work/status"
    sed -n 's/^data: //p' "$work/body" | grep . || cat "$work/body"
}

# waits until <path> answers 200 for <token> (events between services are asynchronous).
eventually_ok() {
    local path=$1 token=$2 what=$3
    for _ in $(seq 1 30); do
        call x GET "$path" '' "$token" >/dev/null
        [[ $status == 200 ]] && { echo "ok  $what"; return; }
        sleep 1
    done
    fail "$what: $path still $status"
}

step "Stack is up"
call x GET / >/dev/null; expect 200 "web app"
call x GET /signin/password >/dev/null; expect 200 "web app, a deep link (SPA fallback)"
call x GET /.well-known/openid-configuration | jq -e --arg iss "$base" '.issuer == $iss' >/dev/null
expect 200 "Auth discovery names issuer $base"

step "First Owner from the bootstrap invitation"
link=$(mail_link "$owner_email" invite)
token=${link##*/}
call owner GET "/auth/invitations/$token" >/dev/null; expect 200 "bootstrap invitation is valid"
access=$(call owner POST "/auth/invitations/$token/accept" \
    "{\"password\":\"$password\",\"displayName\":\"Owner\",\"clientId\":\"$client\"}" | jq -r .accessToken)
expect 2xx "Owner registers with a password"

step "Sign in, refresh, profile"
access=$(call owner POST /auth/login "{\"email\":\"$owner_email\",\"password\":\"$password\",\"clientId\":\"$client\"}" | jq -r .accessToken)
expect 200 "Owner signs in"
access=$(call owner POST /auth/refresh "{\"clientId\":\"$client\"}" | jq -r .accessToken)
expect 200 "refresh from the cookie"
cp "$work/owner" "$work/owner-before-reload"
call owner POST /auth/refresh "{\"clientId\":\"$client\"}" >/dev/null; expect 200 "refresh whose answer the browser loses (page reload)"
cp "$work/owner-before-reload" "$work/owner"
access=$(call owner POST /auth/refresh "{\"clientId\":\"$client\"}" | jq -r .accessToken)
expect 200 "the same old cookie again inside the grace window (R6a)"
eventually_ok /users/me "$access" "Accounts created the profile from Auth's event"
call owner GET /users/me '' "$access" | jq -e '.role == "Owner"' >/dev/null || fail "Owner role did not reach Accounts"
echo "ok  role Owner in Accounts"

step "Household"
household=$(call owner POST /households '{"name":"Dom testowy"}' "$access" | jq -r .id)
expect 2xx "Owner creates a household"
code=$(call owner POST "/households/$household/invites" '{"role":"Member"}' "$access" | jq -r .code)
expect 2xx "Owner creates a household invite"

step "Second person: app invitation, sign-up, joins the household"
call owner POST /auth/invitations "{\"email\":\"$member_email\"}" "$access" >/dev/null
expect 2xx "Owner invites $member_email to the app"
link=$(mail_link "$member_email" invite)
member=$(call member POST "/auth/invitations/${link##*/}/accept" \
    "{\"password\":\"$password\",\"displayName\":\"Member\",\"clientId\":\"$client\"}" | jq -r .accessToken)
expect 2xx "member registers"
eventually_ok /users/me "$member" "member profile in Accounts"
for _ in $(seq 1 30); do
    call member POST "/invites/$code/accept" '' "$member" >/dev/null
    [[ $status == 2* ]] && break
    sleep 1
done
[[ $status == 2* ]] || fail "member accepts the household invite: $status $(cat "$work/body")"
echo "ok  member accepts the household invite ($status)"
count=$(call owner GET "/households/$household/members" '' "$access" | jq '[.. | objects | select(has("userId"))] | length')
expect 200 "Owner lists members"
[[ $count -ge 2 ]] || fail "expected 2 members, got $count"
echo "ok  $count members"

step "Notifications"
call owner GET /notifications/vapid-public-key '' "$access" | jq -e '.publicKey | length == 87' >/dev/null
expect 200 "the instance's VAPID public key"
call owner PUT /notifications/mutes "{\"householdId\":\"$household\",\"source\":null}" "$access" >/dev/null
expect 2xx "Owner mutes the household"
call owner GET /notifications/settings '' "$access" | jq -e --arg h "$household" '.mutes[0].householdId == $h' >/dev/null
expect 200 "the mute is in the settings"
call owner POST /notifications/test '' "$access" >/dev/null; expect 409 "a test notification without devices"

step "HomeMix"
call owner GET /homemix/api/store '' "$access" | jq -e 'any(.[]; .official and .addonId == "pogoda")' >/dev/null
expect 200 "the store lists the official Pogoda"
sha=$(call owner POST /homemix/api/packages/official/pogoda '' "$access" | jq -r .sha256)
expect 2xx "Owner adds the official Pogoda package"
consent='{"capabilities":["storage.household"],"network":["https://api.open-meteo.com","https://geocoding-api.open-meteo.com"],"jarvis":true}'
call owner POST "/homemix/api/households/$household/installations" "{\"sha256\":\"$sha\",\"consent\":$consent}" "$access" >/dev/null
expect 2xx "Owner installs Pogoda in the household, JARVIS allowed"
call owner GET "/homemix/api/households/$household/installations" '' "$access" | jq -e 'any(.[]; .addonId == "pogoda")' >/dev/null
expect 200 "the household lists Pogoda"

step "JARVIS"
call x GET /.well-known/oauth-protected-resource/jarvis/mcp | jq -e --arg r "$base/jarvis/mcp" '.resource == $r' >/dev/null
expect 200 "the gateway's resource metadata (RFC 9728)"
call x GET /.well-known/oauth-authorization-server | jq -e '.registration_endpoint | endswith("/auth/oauth/register")' >/dev/null
expect 200 "Auth offers dynamic registration for connected apps"
mcp tools/list '{}' >/dev/null; expect 401 "the gateway without a token"
grep -qi 'resource_metadata' <("${curl_base[@]}" -o /dev/null -D - -X POST -H 'Content-Type: application/json' \
    -H 'Accept: application/json, text/event-stream' --data '{"jsonrpc":"2.0","id":1,"method":"tools/list"}' "$base/jarvis/mcp") \
    || fail "401 without WWW-Authenticate resource_metadata"
echo "ok  401 names the resource metadata"
tools=$(mcp tools/list '{}' "$access" | jq -r '.result.tools[].name')
expect 200 "tools/list through the gateway"
for name in households_my_households accounts_my_profile pogoda_forecast; do
    grep -qx "$name" <<<"$tools" || fail "the gateway has no $name: $(tr '\n' ' ' <<<"$tools")"
    echo "ok  tool $name"
done
mcp tools/call '{"name":"households_my_households","arguments":{}}' "$access" | grep -q 'Dom testowy' \
    || fail "households_my_households through the gateway: $(cat "$work/body")"
echo "ok  a tool call through the gateway (as an assistant, delegated token)"
conversation=$(call owner POST /jarvis/api/conversations '{}' "$access" | jq -r .id)
expect 2xx "Owner starts a conversation"
call owner POST "/jarvis/api/conversations/$conversation/messages" \
    "{\"text\":\"Jarvis, jaka jutro pogoda?\",\"householdId\":\"$household\"}" "$access" > "$work/answer"
expect 200 "a message to JARVIS (server-sent events)"
grep -q '^event: message' "$work/answer" || fail "no answer from JARVIS: $(cat "$work/answer")"
echo "ok  JARVIS answers: $(sed -n '/^event: message/{n;p}' "$work/answer" | sed 's/^data: //' | jq -r '.text' | head -c 300)"

step "Sign out"
call owner POST /auth/logout '{}' >/dev/null; expect 204 "Owner signs out"
call owner POST /auth/refresh "{\"clientId\":\"$client\"}" >/dev/null; expect 401 "refresh after sign-out is refused"

printf '\nSMOKE TEST PASSED\n'
