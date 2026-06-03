FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Wolfremium.Cli/Wolfremium.Cli.csproj", "Wolfremium.Cli/"]
RUN dotnet restore "Wolfremium.Cli/Wolfremium.Cli.csproj"
COPY . .
WORKDIR "/src/Wolfremium.Cli"
RUN dotnet build "./Wolfremium.Cli.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Wolfremium.Cli.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Wolfremium.Cli.dll"]
