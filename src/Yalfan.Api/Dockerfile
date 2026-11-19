# Build context is the repository root:
#   docker build -f src/Yalfan.Api/Dockerfile .
# This image is for local development. Production hardening is roadmap task 3.1.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Project files first, so the restore layer is reused until a dependency changes.
# .editorconfig carries the analyzer exceptions (ADR 0015); without it the build fails here.
COPY global.json Directory.Build.props Directory.Packages.props BannedSymbols.txt .editorconfig ./
COPY src/Yalfan.Domain/Yalfan.Domain.csproj src/Yalfan.Domain/
COPY src/Yalfan.Application/Yalfan.Application.csproj src/Yalfan.Application/
COPY src/Yalfan.Infrastructure/Yalfan.Infrastructure.csproj src/Yalfan.Infrastructure/
COPY src/Yalfan.Api/Yalfan.Api.csproj src/Yalfan.Api/
RUN dotnet restore src/Yalfan.Api/Yalfan.Api.csproj

COPY src/ src/
RUN dotnet publish src/Yalfan.Api/Yalfan.Api.csproj --configuration Release --no-restore --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

# The base image provides a non-root user and listens on 8080.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Yalfan.Api.dll"]
