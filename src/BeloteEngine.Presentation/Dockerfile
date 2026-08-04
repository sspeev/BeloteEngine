FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/BeloteEngine.Presentation/BeloteEngine.Presentation.csproj", "BeloteEngine.Presentation/"]
COPY ["src/BeloteEngine.Infrastructure/BeloteEngine.Infrastructure.csproj", "BeloteEngine.Infrastructure/"]
COPY ["src/BeloteEngine.Application/BeloteEngine.Application.csproj", "BeloteEngine.Application/"]
COPY ["src/BeloteEngine.Domain/BeloteEngine.Domain.csproj", "BeloteEngine.Domain/"]

RUN dotnet restore "BeloteEngine.Presentation/BeloteEngine.Presentation.csproj" \
    /p:SpaProxyLaunchEnabled=false

COPY src/ .

WORKDIR "/src/BeloteEngine.Presentation"
RUN dotnet build "BeloteEngine.Presentation.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/build \
    /p:SpaProxyLaunchEnabled=false

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "BeloteEngine.Presentation.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false \
    /p:SpaProxyLaunchEnabled=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "BeloteEngine.Presentation.dll"]
