#!/bin/sh
# Builds every official add-on (homemix/official/*) into OUT as <id>.zip. The HomeMix image runs it so the store has
# them without GitHub (docs/homemix.md, "The store"); run it by hand to try them in a local HomeMix
# (HomeMix__OfficialPath=<OUT>). Needs Node 24 and the repository's src/Web.
set -eu
root=$(cd "$(dirname "$0")/../.." && pwd)
out=${1:-"$root/homemix/official/dist"}
mkdir -p "$out"
out=$(cd "$out" && pwd)

# The SDK and @homemix/vue build with src/Web's TypeScript and Vue.
(cd "$root/src/Web" && npm ci --no-audit --no-fund)
(cd "$root/src/Web/homemix" && npm ci --no-audit --no-fund && npm run build)

for dir in "$root"/homemix/official/*/; do
  [ -f "$dir/public/homemix.json" ] || continue
  id=$(node -p "require('$dir/public/homemix.json').id")
  (cd "$dir" && npm ci --no-audit --no-fund && npx vite build && npx homemix pack --out="$out/$id.zip")
done
