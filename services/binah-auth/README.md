# Binah Auth Service

> **📚 For detailed technical documentation, see [docs/services/binah-auth.md](../../docs/services/binah-auth.md)**

**Port:** 8093
**Tech Stack:** .NET 8, ASP.NET Core, PostgreSQL
**Purpose:** Authentication and user management

## Overview

The Binah Auth Service handles all authentication, authorization, and user management for the Binelek platform.

## Features

- User registration and login
- JWT token generation and validation
- Refresh token management
- Password reset and change
- Company/tenant management
- Role-based access control (RBAC)

## Running Locally

### Prerequisites
- .NET 8 SDK
- PostgreSQL database

### Development Mode

```bash
cd services/binah-auth
dotnet restore
dotnet run
```

The service will be available at `http://localhost:8093`

## API Endpoints

```
POST   /api/auth/register          - Register new user
POST   /api/auth/login             - Login and get JWT
POST   /api/auth/refresh           - Refresh access token
POST   /api/auth/change-password   - Change password
POST   /api/auth/reset-password    - Reset password
GET    /api/auth/me                - Get current user
GET    /api/users                  - List users
GET    /api/users/{id}             - Get user by ID
PUT    /api/users/{id}             - Update user
DELETE /api/users/{id}             - Delete user
```

## Configuration

### Environment Variables

```bash
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:8093
ConnectionStrings__AuthDb=Host=localhost;Database=binah_auth;Username=postgres;Password=postgres
JWT_SECRET=your-secret-key-here
JWT_ISSUER=binah-auth
JWT_AUDIENCE=binah-clients
JWT_EXPIRATION_MINUTES=60
```

## Database

Uses PostgreSQL for user and company data storage.

### Migrations

```bash
cd services/binah-auth
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## Security

- Passwords are hashed using bcrypt
- JWT tokens with configurable expiration
- Refresh tokens for long-lived sessions
- Role-based authorization

## Related Services

- [binah-api](../binah-api/README.md) - API Gateway

## License

Internal use only - Binelek Platform
