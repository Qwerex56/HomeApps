# Build from the repository root: docker build -f src/ServiceName/ServiceName.Api/Dockerfile .
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /repo
COPY src/ src/
RUN dotnet restore src/ServiceName/ServiceName.Api/ServiceName.Api.csproj
RUN dotnet publish src/ServiceName/ServiceName.Api/ServiceName.Api.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ServiceName.Api.dll"]
