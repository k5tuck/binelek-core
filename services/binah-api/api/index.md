# API Reference

This section contains the auto-generated API reference documentation for the Binah API Gateway.

## Namespaces

The API is organized into the following namespaces:

- **Binah.API.Controllers** - REST API controllers
- **Binah.API.Services** - Gateway services
- **Binah.API.Middleware** - Request processing middleware
- **Binah.API.Hubs** - SignalR real-time hubs
- **Binah.API.Plugins** - Plugin system
- **Binah.API.Models** - Data models

## Key Controllers

- **ProxyController** - Routes requests to backend services
- **HealthController** - Health check endpoints
- **MetricsController** - Prometheus metrics
- **ConfigController** - Gateway configuration

## Middleware Pipeline

The gateway processes requests through middleware:

1. **Rate Limiting** - Check request quotas
2. **Authentication** - Validate JWT token
3. **Compression** - Compress responses
4. **Logging** - Log request/response
5. **Circuit Breaker** - Handle service failures
6. **Routing** - Forward to backend service

## SignalR Hubs

Real-time communication hubs:

- **NotificationHub** - Push notifications to clients
- **EventHub** - Event streaming

## Plugin System

The gateway supports custom plugins for:
- Request transformation
- Response caching
- Custom authentication
- Metrics collection

---

*This documentation is automatically generated from XML comments in the source code.*
