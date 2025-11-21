# Binah API Gateway - API Documentation

Welcome to the Binah API Gateway documentation.

## Overview

The Binah API Gateway is the main entry point for all client requests to the Binelek platform. It provides routing, rate limiting, authentication, API versioning, and aggregation of backend microservices.

## Key Features

- **API Gateway** - Central entry point for all services
- **Rate Limiting** - Request throttling and quota management
- **API Versioning** - Version management and routing
- **Authentication** - JWT token validation
- **Response Compression** - Gzip/Brotli compression
- **Circuit Breaker** - Polly-based resilience patterns
- **Plugin System** - Extensible middleware architecture
- **WebSocket Support** - Real-time SignalR hubs

## API Reference

For detailed API documentation, see the [API Reference](api/index.md).

## Architecture

The service is built with:
- **.NET 8.0** - Modern C# web API
- **ASP.NET Core** - Web framework
- **Polly** - Resilience and transient fault handling
- **SignalR** - Real-time communication
- **Swagger/OpenAPI** - API documentation
- **Prometheus** - Metrics and monitoring

## API Versioning

The gateway supports multiple API versions:
- **v1** - Current stable version
- **v2** - Beta features (experimental)

Version is specified in the URL: `/api/v1/...` or via header: `api-version: 1.0`

## Getting Started

To get started with the Binah API Gateway, refer to the controller documentation:

- [ProxyController](api/Binah.API.Controllers.ProxyController.yml) - Service routing and proxying
- [HealthController](api/Binah.API.Controllers.HealthController.yml) - Health checks
- [MetricsController](api/Binah.API.Controllers.MetricsController.yml) - Prometheus metrics

## Related Services

The API Gateway routes to:
- **binah-auth** - Authentication service
- **binah-ontology** - Ontology service
- **binah-aip** - AI processing service
- All other Binelek microservices

---

**Version:** 1.0.0
**Last Updated:** 2025-11-21
