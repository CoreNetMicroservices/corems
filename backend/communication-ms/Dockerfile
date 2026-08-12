FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE 5101

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
ARG GITHUB_TOKEN
WORKDIR /src

# Copy build configuration
COPY Directory.Build.props Directory.Packages.props nuget.config ./

# Authenticate to GitHub Packages
RUN dotnet nuget update source github \
    --username "docker" \
    --password "$GITHUB_TOKEN" \
    --store-password-in-clear-text

# Copy only this service's project files for restore
COPY communication-ms/src/CoreMs.CommunicationMs.Api/CoreMs.CommunicationMs.Api.csproj communication-ms/src/CoreMs.CommunicationMs.Api/
COPY communication-ms/src/CoreMs.CommunicationMs.Core/CoreMs.CommunicationMs.Core.csproj communication-ms/src/CoreMs.CommunicationMs.Core/
COPY communication-ms/src/CoreMs.CommunicationMs.Client/CoreMs.CommunicationMs.Client.csproj communication-ms/src/CoreMs.CommunicationMs.Client/
COPY communication-ms/src/CoreMs.CommunicationMs.Infrastructure/CoreMs.CommunicationMs.Infrastructure.csproj communication-ms/src/CoreMs.CommunicationMs.Infrastructure/

# Restore with package references from GitHub Packages
RUN dotnet restore communication-ms/src/CoreMs.CommunicationMs.Api/CoreMs.CommunicationMs.Api.csproj \
    /p:UsePackageReferences=true

# Copy service source and publish
COPY communication-ms/src/ communication-ms/src/
RUN dotnet publish communication-ms/src/CoreMs.CommunicationMs.Api/CoreMs.CommunicationMs.Api.csproj \
    -c Release -o /app/publish --no-restore \
    /p:UsePackageReferences=true

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5101
ENTRYPOINT ["dotnet", "CoreMs.CommunicationMs.Api.dll"]
