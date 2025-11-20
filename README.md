# binelek-core

Core platform services providing authentication, knowledge graph, and API gateway

## Services Included

### 1. binah-auth (Port 8093)
Authentication, JWT tokens, multi-tenant management

### 2. binah-ontology (Port 8091)  
Knowledge graph, Neo4j operations, Kafka event producer

### 3. binah-api (Port 8092)
API Gateway, routing, SignalR real-time updates

## Quick Start

```bash
# Clone repository
git clone https://github.com/k5tuck/binelek-core.git
cd binelek-core

# Build all services
dotnet build

# Run tests
dotnet test

# Run with Docker Compose
docker-compose up
```

## Repository Structure

```
binelek-core/
├── src/              # Source code for all services
├── tests/            # Unit and integration tests  
├── docs/             # Documentation
├── deployment/       # Docker and Kubernetes configs
└── README.md         # This file
```

## Dependencies

- **binelek-shared** - Shared libraries (NuGet packages)
- .NET 8.0 SDK
- Docker (for local development)

## Documentation

See [docs/](docs/) for detailed documentation on each service.

## License

MIT License - See [LICENSE](LICENSE)
