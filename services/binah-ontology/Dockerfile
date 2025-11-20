# Multi-stage build for Binah Ontology Service
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 8088
ENV ASPNETCORE_URLS=http://+:8088

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy library projects
COPY ["libraries/binah-contracts/Binah.Contracts.csproj", "libraries/binah-contracts/"]
COPY ["libraries/binah-core/Binah.Core.csproj", "libraries/binah-core/"]
COPY ["libraries/binah-infrastructure/Binah.Infrastructure.csproj", "libraries/binah-infrastructure/"]

# Copy service project
COPY ["services/binah-ontology/Binah.Ontology.csproj", "services/binah-ontology/"]
RUN dotnet restore "services/binah-ontology/Binah.Ontology.csproj"

# Copy source files
COPY libraries/binah-contracts/ libraries/binah-contracts/
COPY libraries/binah-core/ libraries/binah-core/
COPY libraries/binah-infrastructure/ libraries/binah-infrastructure/
COPY services/binah-ontology/ services/binah-ontology/

WORKDIR "/src/services/binah-ontology"
RUN dotnet build "Binah.Ontology.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Binah.Ontology.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Binah.Ontology.dll"]
