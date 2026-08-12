FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE 5104

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
COPY template-ms/src/CoreMs.TemplateMs.Api/CoreMs.TemplateMs.Api.csproj template-ms/src/CoreMs.TemplateMs.Api/
COPY template-ms/src/CoreMs.TemplateMs.Core/CoreMs.TemplateMs.Core.csproj template-ms/src/CoreMs.TemplateMs.Core/
COPY template-ms/src/CoreMs.TemplateMs.Client/CoreMs.TemplateMs.Client.csproj template-ms/src/CoreMs.TemplateMs.Client/
COPY template-ms/src/CoreMs.TemplateMs.Infrastructure/CoreMs.TemplateMs.Infrastructure.csproj template-ms/src/CoreMs.TemplateMs.Infrastructure/

# Restore with package references from GitHub Packages
RUN dotnet restore template-ms/src/CoreMs.TemplateMs.Api/CoreMs.TemplateMs.Api.csproj \
    /p:UsePackageReferences=true

# Copy service source and publish
COPY template-ms/src/ template-ms/src/
RUN dotnet publish template-ms/src/CoreMs.TemplateMs.Api/CoreMs.TemplateMs.Api.csproj \
    -c Release -o /app/publish --no-restore \
    /p:UsePackageReferences=true

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5104
ENTRYPOINT ["dotnet", "CoreMs.TemplateMs.Api.dll"]
