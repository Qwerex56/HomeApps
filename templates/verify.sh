#!/usr/bin/env bash
# Proves the templates still produce working services. CI runs this; you can too.
#
# 1. Copies the repository into a temporary folder (your checkout is never touched).
# 2. Generates "Ping" with the Echo sample and an empty "Blank" service, plus command and query slices.
# 3. Builds the solution and runs every generated test project. Integration tests need Docker;
#    pass --no-docker to skip them.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
filter=""
if [[ "${1:-}" == "--no-docker" ]]; then
  filter="--filter Category!=Integration"
fi

work="$(mktemp -d)"
trap 'rm -rf "$work"; dotnet new uninstall "$repo_root/templates" >/dev/null 2>&1 || true' EXIT

echo "==> Copying the repository to $work"
(cd "$repo_root" && git ls-files -co --exclude-standard | tar -cf - -T -) | tar -xf - -C "$work"

echo "==> Installing the templates"
dotnet new uninstall "$repo_root/templates" >/dev/null 2>&1 || true
dotnet new install "$repo_root/templates" >/dev/null

cd "$work"
echo "==> Generating services"
dotnet new homeapps-service -n Ping --sample
dotnet new homeapps-service -n Blank

echo "==> Generating slices"
(cd src/Blank \
  && dotnet new homeapps-command -n CreateThing --service Blank --feature Things --returns Guid \
  && dotnet new homeapps-command -n DeleteThing --service Blank --feature Things --method Delete \
  && dotnet new homeapps-query -n GetThing --service Blank --feature Things)

echo "==> Building"
solution="$(find . -maxdepth 1 \( -name '*.slnx' -o -name '*.sln' \) | sort | head -n 1)"
# New projects already treat warnings as errors (src/Directory.Build.props).
dotnet build "$solution"

echo "==> Testing"
for project in tests/Ping/*/*.csproj tests/Blank/*/*.csproj; do
  # shellcheck disable=SC2086
  dotnet test "$project" --no-build $filter
done

echo "==> Templates verified"
