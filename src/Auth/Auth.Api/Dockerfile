# Build from the repository root: docker build -f src/Auth/Auth.Api/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/ src/
RUN dotnet publish src/Auth/Auth.Api/Auth.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
# Development signing key and Data Protection keys; mount a volume here so they survive a new container.
RUN mkdir -p /app/.keys && chown "$APP_UID" /app/.keys
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Auth.Api.dll"]
