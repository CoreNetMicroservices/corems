FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE 5103

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
ARG GITHUB_TOKEN
WORKDIR /src

# Copy build configuration
COPY Directory.Build.props Directory.Packages.props nuget.config ./

# Write authenticated nuget.config for GitHub Packages
RUN dotnet nuget add source "https://nuget.pkg.github.com/CoreNetMicroservices/index.json" \
    --name github \
    --username "docker" \
    --password "$GITHUB_TOKEN" \
    --store-password-in-clear-text

# Copy only this service's project files for restore
COPY translation-ms/src/CoreMs.TranslationMs.Api/CoreMs.TranslationMs.Api.csproj translation-ms/src/CoreMs.TranslationMs.Api/
COPY translation-ms/src/CoreMs.TranslationMs.Core/CoreMs.TranslationMs.Core.csproj translation-ms/src/CoreMs.TranslationMs.Core/
COPY translation-ms/src/CoreMs.TranslationMs.Infrastructure/CoreMs.TranslationMs.Infrastructure.csproj translation-ms/src/CoreMs.TranslationMs.Infrastructure/

# Restore with package references from GitHub Packages
RUN dotnet restore translation-ms/src/CoreMs.TranslationMs.Api/CoreMs.TranslationMs.Api.csproj \
    /p:UsePackageReferences=true

# Copy service source and publish
COPY translation-ms/src/ translation-ms/src/
RUN dotnet publish translation-ms/src/CoreMs.TranslationMs.Api/CoreMs.TranslationMs.Api.csproj \
    -c Release -o /app/publish --no-restore \
    /p:UsePackageReferences=true

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5103
ENTRYPOINT ["dotnet", "CoreMs.TranslationMs.Api.dll"]
