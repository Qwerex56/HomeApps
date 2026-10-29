#!/usr/bin/env bash
# Packs the kit for plugins that live outside this repository (docs/plugins.md) into a local NuGet feed:
# HomeApps.BuildingBlocks.*, HomeApps.Contracts, HomeApps.Plugins(.Hosting) and the test helpers.
# The version is HomeApps' own (Directory.Build.props). Run it again after changing the kit; a plugin's
# nuget.config points at the feed. HOMEAPPS_PACKAGES changes where the feed is.
set -euo pipefail

cd "$(dirname "$0")/.."
feed="${HOMEAPPS_PACKAGES:-$HOME/.homeapps/packages}"
version=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' Directory.Build.props)

projects=(
    src/BuildingBlocks/HomeApps.BuildingBlocks.Domain/HomeApps.BuildingBlocks.Domain.csproj
    src/BuildingBlocks/HomeApps.BuildingBlocks.Application/HomeApps.BuildingBlocks.Application.csproj
    src/BuildingBlocks/HomeApps.BuildingBlocks.Infrastructure/HomeApps.BuildingBlocks.Infrastructure.csproj
    src/BuildingBlocks/HomeApps.BuildingBlocks.Api/HomeApps.BuildingBlocks.Api.csproj
    src/BuildingBlocks/HomeApps.Plugins/HomeApps.Plugins.csproj
    src/BuildingBlocks/HomeApps.Plugins.Hosting/HomeApps.Plugins.Hosting.csproj
    src/Contracts/HomeApps.Contracts/HomeApps.Contracts.csproj
    tests/BuildingBlocks/HomeApps.BuildingBlocks.Testing/HomeApps.BuildingBlocks.Testing.csproj
)

mkdir -p "$feed"
for project in "${projects[@]}"; do
    dotnet pack "$project" --configuration Release --output "$feed" --nologo --verbosity quiet
    # Packing the same version again: drop the old copy from the NuGet cache, or plugins keep restoring it.
    id=$(basename "$project" .csproj | tr '[:upper:]' '[:lower:]')
    rm -rf "${NUGET_PACKAGES:-$HOME/.nuget/packages}/$id/$version"
done

echo "Packed HomeApps $version into $feed"
