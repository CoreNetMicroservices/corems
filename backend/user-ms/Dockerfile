FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE 5100

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
COPY user-ms/src/CoreMs.UserMs.Api/CoreMs.UserMs.Api.csproj user-ms/src/CoreMs.UserMs.Api/
COPY user-ms/src/CoreMs.UserMs.Core/CoreMs.UserMs.Core.csproj user-ms/src/CoreMs.UserMs.Core/
COPY user-ms/src/CoreMs.UserMs.Infrastructure/CoreMs.UserMs.Infrastructure.csproj user-ms/src/CoreMs.UserMs.Infrastructure/

# Restore with package references from GitHub Packages
RUN dotnet restore user-ms/src/CoreMs.UserMs.Api/CoreMs.UserMs.Api.csproj \
    /p:UsePackageReferences=true

# Copy service source and publish
COPY user-ms/src/ user-ms/src/
RUN dotnet publish user-ms/src/CoreMs.UserMs.Api/CoreMs.UserMs.Api.csproj \
    -c Release -o /app/publish --no-restore \
    /p:UsePackageReferences=true

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5100
ENTRYPOINT ["dotnet", "CoreMs.UserMs.Api.dll"]
