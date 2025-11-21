# Binah Auth Service - API Documentation

Welcome to the Binah Auth Service API documentation.

## Overview

The Binah Auth Service provides authentication and authorization capabilities for the Binelek platform. It manages user authentication, JWT token generation, SAML integration, and role-based access control.

## Key Features

- **JWT Authentication** - Token-based authentication and authorization
- **SAML 2.0 Integration** - Enterprise SSO support
- **User Management** - User registration, login, and password management
- **Role-Based Access Control** - Tenant-level permissions and roles
- **Multi-tenant Support** - Isolated authentication per tenant
- **Password Security** - BCrypt password hashing

## API Reference

For detailed API documentation, see the [API Reference](api/index.md).

## Architecture

The service is built with:
- **.NET 8.0** - Modern C# web API
- **ASP.NET Core Identity** - Authentication framework
- **Entity Framework Core** - ORM for database access
- **PostgreSQL** - User and tenant data storage
- **JWT Bearer** - Token authentication
- **SAML 2.0** - Enterprise SSO integration

## Getting Started

To get started with the Binah Auth Service API, refer to the controller documentation:

- [AuthController](api/Binah.Auth.Controllers.AuthController.yml) - Authentication endpoints
- [UserController](api/Binah.Auth.Controllers.UserController.yml) - User management
- [TenantController](api/Binah.Auth.Controllers.TenantController.yml) - Tenant administration

## Related Services

- **binah-api** - Main API gateway
- **binah-ontology** - Ontology and entity definitions
- All services use binah-auth for authentication

---

**Version:** 1.0.0
**Last Updated:** 2025-11-21
