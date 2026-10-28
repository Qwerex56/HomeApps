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

# call <jar> <method> <path> [json] [token]: prints the body, sets $status.
call() {
    local jar=$1 method=$2 path=$3 body=${4:-} token=${5:-}
    local args=("${curl_base[@]}" -b "$work/$jar" -c "$work/$jar" -o "$work/body" -w '%{http_code}' -X "$method")
    [[ -n $body ]] && args+=(-H 'Content-Type: application/json' --data "$body")
    [[ -n $token ]] && args+=(-H "Authorization: Bearer $token")
    status=$("${args[@]}" "$base$path")
    cat "$work/body"
}

# expect <status> <what>; "2xx" accepts any success.
expect() { [[ $status == "$1" || ( $1 == 2xx && $status == 2* ) ]] || fail "$2: expected $1, got $status: $(cat "$work/body")"; echo "ok  $2 ($status)"; }

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

step "Sign out"
call owner POST /auth/logout '{}' >/dev/null; expect 204 "Owner signs out"
call owner POST /auth/refresh "{\"clientId\":\"$client\"}" >/dev/null; expect 401 "refresh after sign-out is refused"

printf '\nSMOKE TEST PASSED\n'
