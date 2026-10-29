#!/bin/sh
# Writes /plugins.json, the instance's plugins for the web app (docs/plugins.md), from HOMEAPPS_PLUGINS when the
# container starts: "todo, weather" → ["todo","weather"]. Ids that are not plugin ids are skipped.
set -eu

ids=$(echo "${HOMEAPPS_PLUGINS:-}" | tr ',' '\n' | sed 's/^ *//; s/ *$//' | grep -E '^[a-z][a-z0-9-]{1,31}$' | sed 's/.*/"&"/' | paste -sd, - || true)
echo "[${ids}]" > /usr/share/nginx/html/plugins.json
