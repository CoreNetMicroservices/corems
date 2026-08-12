FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE 5102

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
COPY document-ms/src/CoreMs.DocumentMs.Api/CoreMs.DocumentMs.Api.csproj document-ms/src/CoreMs.DocumentMs.Api/
COPY document-ms/src/CoreMs.DocumentMs.Core/CoreMs.DocumentMs.Core.csproj document-ms/src/CoreMs.DocumentMs.Core/
COPY document-ms/src/CoreMs.DocumentMs.Client/CoreMs.DocumentMs.Client.csproj document-ms/src/CoreMs.DocumentMs.Client/
COPY document-ms/src/CoreMs.DocumentMs.Infrastructure/CoreMs.DocumentMs.Infrastructure.csproj document-ms/src/CoreMs.DocumentMs.Infrastructure/

# Restore with package references from GitHub Packages
RUN dotnet restore document-ms/src/CoreMs.DocumentMs.Api/CoreMs.DocumentMs.Api.csproj \
    /p:UsePackageReferences=true

# Copy service source and publish
COPY document-ms/src/ document-ms/src/
RUN dotnet publish document-ms/src/CoreMs.DocumentMs.Api/CoreMs.DocumentMs.Api.csproj \
    -c Release -o /app/publish --no-restore \
    /p:UsePackageReferences=true

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5102
ENTRYPOINT ["dotnet", "CoreMs.DocumentMs.Api.dll"]
