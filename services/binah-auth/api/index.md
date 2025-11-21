# API Reference

This section contains the auto-generated API reference documentation for the Binah Auth Service.

## Namespaces

The API is organized into the following namespaces:

- **Binah.Auth.Controllers** - REST API controllers
- **Binah.Auth.Services** - Authentication and authorization services
- **Binah.Auth.Models** - Data models and entities
- **Binah.Auth.Repositories** - Data access layer

## Key Controllers

- **AuthController** - Login, logout, token generation, password reset
- **UserController** - User registration and management
- **TenantController** - Tenant administration
- **HealthController** - Health check endpoints
- **MetricsController** - Prometheus metrics

## Authentication Flow

The service implements JWT-based authentication:

1. **Login** - User credentials → JWT token
2. **Token Validation** - Validate JWT on each request
3. **Token Refresh** - Refresh expired tokens
4. **Logout** - Invalidate token

## SAML Integration

For enterprise customers, SAML 2.0 SSO is supported:

- Identity Provider configuration
- Service Provider metadata
- Assertion Consumer Service
- Single Logout Service

---

*This documentation is automatically generated from XML comments in the source code.*
