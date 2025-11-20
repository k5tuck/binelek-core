# Multi-stage build for Binah Auth Service
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 8093
ENV ASPNETCORE_URLS=http://+:8093

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy library projects
COPY ["libraries/binah-contracts/Binah.Contracts.csproj", "libraries/binah-contracts/"]
COPY ["libraries/binah-core/Binah.Core.csproj", "libraries/binah-core/"]

# Copy service project
COPY ["services/binah-auth/Binah.Auth.csproj", "services/binah-auth/"]
RUN dotnet restore "services/binah-auth/Binah.Auth.csproj"

# Copy source files
COPY libraries/binah-contracts/ libraries/binah-contracts/
COPY libraries/binah-core/ libraries/binah-core/
COPY services/binah-auth/ services/binah-auth/

WORKDIR "/src/services/binah-auth"
RUN dotnet build "Binah.Auth.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Binah.Auth.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Binah.Auth.dll"]
