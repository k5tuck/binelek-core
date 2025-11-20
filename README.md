# Binelek Core Services

Core platform services extracted from the Binelek monorepo as part of the repository restructuring initiative.

## Services

This repository contains the 3 core platform services:

| Service | Port | Purpose | Status |
|---------|------|---------|--------|
| **binah-auth** | 8093 | Authentication, JWT tokens, multi-tenant user management | ✅ |
| **binah-ontology** | 8091 | Knowledge graph, Kafka event producer, entity/relationship management | ✅ |
| **binah-api** | 8092 | API Gateway, SignalR hub, request routing, circuit breakers | ✅ |

## Dependencies

These services consume shared libraries from [binelek-shared](https://github.com/k5tuck/binelek-shared):

- **Binah.Core** (v1.0.0-beta.1) - Core utilities, exceptions, middleware
- **Binah.Contracts** (v1.0.0-beta.1) - DTOs, events, Kafka topics
- **Binah.Infrastructure** (v1.0.0-beta.1) - Kafka, multi-tenancy middleware
- **Binah.Domain** (v1.0.0-beta.1) - Domain models, GraphQL schema builder

## Setup

### Prerequisites

- .NET 8.0 SDK
- Docker & Docker Compose (for local development)
- GitHub Personal Access Token with `read:packages` scope

### Configure GitHub Packages

```bash
# Set your GitHub PAT as environment variable
export GITHUB_TOKEN=your_github_pat_here

# The NuGet.config file will automatically use this variable
```

### Build Locally

```bash
# Restore packages
dotnet restore

# Build all services
dotnet build

# Build specific service
dotnet build services/binah-auth/Binah.Auth.csproj
```

### Run with Docker Compose

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop all services
docker-compose down
```

## Project Structure

```
binelek-core/
├── services/
│   ├── binah-auth/        # Authentication service
│   ├── binah-ontology/    # Knowledge graph service
│   └── binah-api/         # API Gateway
├── .github/
│   └── workflows/
│       └── ci-cd.yml      # GitHub Actions CI/CD
├── Binelek.Core.sln       # Solution file
├── NuGet.config           # NuGet package sources
└── README.md
```

## Development

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific service tests
dotnet test services/binah-auth/Tests/Binah.Auth.Tests.csproj
```

### Code Style

- Follow existing code conventions
- Use C# 12 features where appropriate
- Enable nullable reference types
- Write XML documentation for public APIs

## Deployment

### GitHub Actions

All services are automatically built and tested on:
- Push to `main` or `develop` branches
- Pull requests to `main`

### Docker Images

Docker images can be built using the Dockerfiles in each service directory.

## Migration Status

This repository is part of the Week 2 migration from the Binelek monorepo:

- ✅ **Week 2 Day 1:** Shared libraries published to GitHub Packages
- ✅ **Week 2 Day 3:** Core services extracted and configured

**Original monorepo files remain intact** following the copy-first migration strategy.

## Links

- **Shared Libraries:** https://github.com/k5tuck/binelek-shared
- **Packages:** https://github.com/k5tuck?tab=packages
- **Monorepo:** https://github.com/k5tuck/Binelek

## License

MIT License - See LICENSE file for details
