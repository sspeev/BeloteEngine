FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

# ── Build stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy only project files first for layer-cached restore
COPY ["BeloteEngine.Api/BeloteEngine.Api.csproj",      "BeloteEngine.Api/"]
COPY ["BeloteEngine.Services/BeloteEngine.Services.csproj", "BeloteEngine.Services/"]
COPY ["BeloteEngine.Data/BeloteEngine.Data.csproj",    "BeloteEngine.Data/"]

# Restore with SpaProxy disabled so it doesn't try to launch npm in CI/Docker
RUN dotnet restore "BeloteEngine.Api/BeloteEngine.Api.csproj" \
    /p:SpaProxyLaunchEnabled=false

# Copy the rest of the source
COPY . .

WORKDIR "/src/BeloteEngine.Api"
RUN dotnet build "BeloteEngine.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/build \
    /p:SpaProxyLaunchEnabled=false

# ── Publish stage ─────────────────────────────────────────────────────────────
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "BeloteEngine.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false \
    /p:SpaProxyLaunchEnabled=false

# ── Final stage ───────────────────────────────────────────────────────────────
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Tell Kestrel to listen on plain HTTP inside the container.
# HTTPS termination is handled by the platform reverse proxy (for example Cloud Run).
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "BeloteEngine.Api.dll"]
