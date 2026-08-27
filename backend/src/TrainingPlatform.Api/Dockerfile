FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the whole backend so Directory.Build.props / Directory.Packages.props and the
# inter-project references resolve correctly (mirrors how the sibling JobBoard.Platform
# project's Dockerfile handles the same shared-props layout).
COPY . .

RUN dotnet restore src/TrainingPlatform.Api/TrainingPlatform.Api.csproj
RUN dotnet publish src/TrainingPlatform.Api/TrainingPlatform.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl is only needed for the HEALTHCHECK below.
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "TrainingPlatform.Api.dll"]
