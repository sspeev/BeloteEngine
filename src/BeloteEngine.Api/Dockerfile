FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8081

# This stage is used to build the service project
FROM mcr.microsoft. com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/BeloteEngine.Api/BeloteEngine.Api. csproj", "src/BeloteEngine.Api/"]
COPY ["src/BeloteEngine.Services/BeloteEngine.Services.csproj", "src/BeloteEngine.Services/"]
COPY ["src/BeloteEngine.Data/BeloteEngine.Data.csproj", "src/BeloteEngine.Data/"]
RUN dotnet restore "./src/BeloteEngine.Api/BeloteEngine.Api.csproj"
COPY . .
WORKDIR "/src/src/BeloteEngine.Api"
RUN dotnet build "./BeloteEngine.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./BeloteEngine. Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish . 
ENTRYPOINT ["dotnet", "BeloteEngine.Api.dll"]