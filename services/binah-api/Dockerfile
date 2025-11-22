# Multi-stage build for Binah API Gateway
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 8092
ENV ASPNETCORE_URLS=http://+:8092

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG GITHUB_TOKEN
WORKDIR /src

# Configure GitHub Packages as NuGet source
RUN if [ -n "$GITHUB_TOKEN" ]; then \
      dotnet nuget add source https://nuget.pkg.github.com/k5tuck/index.json \
        --name github \
        --username k5tuck \
        --password "$GITHUB_TOKEN" \
        --store-password-in-clear-text; \
    fi

# Copy service project
COPY ["services/binah-api/Binah.API.csproj", "services/binah-api/"]
RUN dotnet restore "services/binah-api/Binah.API.csproj"

# Copy source files
COPY services/binah-api/ services/binah-api/

WORKDIR "/src/services/binah-api"
RUN dotnet build "Binah.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Binah.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Binah.API.dll"]
