FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
# curl is here for the HEALTHCHECK below, not for the app.
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 curl && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 8080

# Liveness, not readiness: /healthz touches no database, so a database outage does not make
# the container unhealthy and send it into a restart loop while someone is trying to diagnose
# it. start-period covers the schema check the API does before it listens.
HEALTHCHECK --interval=30s --timeout=3s --start-period=30s --retries=3 \
    CMD curl -fsS http://localhost:8080/healthz || exit 1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["EduCore.csproj", "./"]
RUN dotnet restore "EduCore.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "./EduCore.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./EduCore.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EduCore.dll"]
