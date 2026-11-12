# Build from the repository root: docker build -f src/HomeMix/HomeMix.Api/Dockerfile .

# The official add-ons (homemix/official/*) as zips, so the store offers them without GitHub (docs/homemix.md).
FROM node:24-alpine AS official
WORKDIR /repo
COPY src/Web/ src/Web/
COPY homemix/official/ homemix/official/
RUN sh homemix/official/build.sh /official

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/ src/
RUN dotnet publish src/HomeMix/HomeMix.Api/HomeMix.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=official /official /app/official
# The packages volume is mounted here; created now so it belongs to the app user.
RUN mkdir -p /data/packages && chown -R $APP_UID /data
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "HomeMix.Api.dll"]
