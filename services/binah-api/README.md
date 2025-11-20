# Binah API Gateway

> **📚 For detailed technical documentation, see [docs/services/binah-api.md](../../docs/services/binah-api.md)**

The API Gateway serves as the central entry point for all client requests to the Binah platform. It handles authentication, authorization, rate limiting, request routing, and provides a unified interface for all microservices.

## Features

- **Unified Authentication**: JWT-based authentication for all services
- **Request Routing**: Intelligent routing to microservices (Ontology, Context, Auth, AI, LLM)
- **Rate Limiting**: Protects services from overload
- **Circuit Breaker**: Prevents cascading failures with Polly resilience patterns
- **CORS Support**: Configurable CORS for web clients
- **API Versioning**: Support for multiple API versions
- **Health Checks**: Monitor gateway and downstream service health
- **Request/Response Logging**: Correlation ID tracking and structured logging
- **Swagger Documentation**: Interactive API documentation

## Architecture

```
Client → API Gateway (Port 8092) → Microservices
                ↓
        ┌───────────────┐
        │ Authentication│
        ├───────────────┤
        │ Rate Limiting │
        ├───────────────┤
        │ Routing       │
        └───────────────┘
                ↓
    ┌───────┬────────┬────────┐
    │       │        │        │
Ontology Context  Auth    AI/LLM
  :5001    :5003   :5002  :5004/5005
```

## Prerequisites

- .NET 8.0 SDK
- Running microservices:
  - Binah.Ontology (http://localhost:5001)
  - Binah.Auth (http://localhost:5002)
  - Binah.Context (http://localhost:5003)
  - Binah.AI (optional, http://localhost:5004)
  - Binah.LLM (optional, http://localhost:5005)

## Configuration

Edit `appsettings.json` to configure services and settings:

```json
{
  "Authentication": {
    "Jwt": {
      "Issuer": "binah-api",
      "Audience": "binah-clients",
      "SecretKey": "your-secret-key-here",
      "TokenExpirationMinutes": 60
    }
  },
  "Services": {
    "Ontology": {
      "Url": "http://localhost:5001",
      "Timeout": 30
    },
    "Context": {
      "Url": "http://localhost:5003",
      "Timeout": 30
    },
    "Auth": {
      "Url": "http://localhost:5002",
      "Timeout": 30
    }
  },
  "RateLimit": {
    "RequestsPerMinute": 100
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000"
    ]
  }
}
```

### Important Configuration Notes:

1. **JWT Secret Key**: Change the default secret key in production. Use at least 32 characters.
2. **Service URLs**: Update URLs to match your deployment
3. **Rate Limiting**: Adjust based on your needs and capacity
4. **CORS**: Add your frontend URLs to allowed origins

## Running the Gateway

### Development

```bash
cd services/binah-api
dotnet run
```

The gateway will start on `http://localhost:8092` (or configured port).

### Production

```bash
cd services/binah-api
dotnet publish -c Release -o publish
cd publish
dotnet Binah.API.dll
```

## API Endpoints

### Authentication (Proxied to Auth Service)

```http
POST /api/auth/register     - Register new user
POST /api/auth/login        - Login and get JWT token
POST /api/auth/refresh      - Refresh access token
POST /api/auth/revoke       - Revoke refresh token
GET  /api/auth/me           - Get current user info
POST /api/auth/change-password - Change password
POST /api/auth/reset-password  - Reset password
```

### Ontology (Proxied to Ontology Service)

```http
GET    /api/ontology/entities/{type}           - Get entities by type
GET    /api/ontology/entities/{id}             - Get entity by ID
POST   /api/ontology/entities                  - Create entity
PUT    /api/ontology/entities/{id}             - Update entity
DELETE /api/ontology/entities/{id}             - Delete entity
POST   /api/ontology/relationships             - Create relationship
GET    /api/ontology/entities/{id}/relationships - Get entity relationships
POST   /api/ontology/query                     - Execute Cypher query
```

### Context (Proxied to Context Service)

```http
POST   /api/context/embeddings           - Create embedding
POST   /api/context/embeddings/batch     - Create batch embeddings
POST   /api/context/enrich               - Enrich entity
POST   /api/context/search               - Semantic search
DELETE /api/context/embeddings/{id}      - Delete embedding
GET    /api/context/embeddings/{id}/metadata - Get embedding metadata
GET    /api/context/statistics/enrichment    - Get enrichment statistics
GET    /api/context/health               - Context service health
```

### Gateway

```http
GET  /                  - Gateway info
GET  /health            - Gateway health check
```

## Authentication Flow

### 1. Register

```http
POST /api/auth/register
Content-Type: application/json

{
  "username": "john.doe",
  "email": "john@example.com",
  "password": "SecurePassword123!",
  "firstName": "John",
  "lastName": "Doe"
}
```

### 2. Login

```http
POST /api/auth/login
Content-Type: application/json

{
  "usernameOrEmail": "john.doe",
  "password": "SecurePassword123!"
}
```

Response:
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGc...",
    "refreshToken": "refresh_token_here",
    "expiresIn": 3600
  }
}
```

### 3. Use Token

Include the JWT token in the Authorization header for all authenticated requests:

```http
GET /api/ontology/entities/Person
Authorization: Bearer eyJhbGc...
```

## Rate Limiting

The gateway implements rate limiting to protect services from overload:

- **Default**: 100 requests per minute per client
- **Development**: 1000 requests per minute
- **Headers**: 
  - `X-RateLimit-Limit`: Maximum requests allowed
  - `X-RateLimit-Remaining`: Requests remaining
  - `Retry-After`: Seconds to wait when limit exceeded

When rate limit is exceeded, you'll receive a `429 Too Many Requests` response.

## Resilience Patterns

The gateway uses Polly to implement resilience patterns:

### Retry Policy
- Retries 3 times with exponential backoff (2s, 4s, 8s)
- Applied to transient HTTP errors (5xx, network issues)

### Circuit Breaker
- Opens after 5 consecutive failures
- Stays open for 30 seconds
- Prevents cascading failures

## Error Handling

The gateway provides consistent error responses:

```json
{
  "success": false,
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable error message",
    "details": {}
  }
}
```

Common HTTP status codes:
- `200 OK`: Successful request
- `201 Created`: Resource created successfully
- `400 Bad Request`: Invalid request data
- `401 Unauthorized`: Missing or invalid token
- `403 Forbidden`: Insufficient permissions
- `404 Not Found`: Resource not found
- `429 Too Many Requests`: Rate limit exceeded
- `500 Internal Server Error`: Server error
- `503 Service Unavailable`: Service down or circuit breaker open

## Monitoring

### Health Checks

```http
GET /health
```

Returns health status of the gateway.

### Logs

The gateway uses Serilog for structured logging with:
- Correlation IDs for request tracing
- Environment enrichment
- Thread ID tracking
- Console and file outputs

Log levels:
- **Debug**: Detailed debugging information
- **Information**: General informational messages
- **Warning**: Warning messages
- **Error**: Error messages
- **Fatal**: Fatal errors causing shutdown

## CORS Configuration

Configure CORS to allow your frontend applications:

```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "https://your-app.com"
    ]
  }
}
```

## API Versioning

The gateway supports API versioning:
- Current version: v1
- Version can be specified in URL: `/api/v1/ontology/entities`
- Default version assumed if not specified

## Security Best Practices

1. **JWT Secret Key**: Use a strong, random secret key (32+ characters)
2. **HTTPS**: Always use HTTPS in production
3. **CORS**: Restrict allowed origins to known domains
4. **Rate Limiting**: Adjust limits based on your capacity
5. **Secrets Management**: Use environment variables or secret management tools
6. **Token Expiration**: Keep access token expiration short (15-60 minutes)

## Troubleshooting

### Service Unavailable (503)

**Cause**: Downstream service is not running or circuit breaker is open

**Solution**: 
- Ensure all services are running
- Check service health endpoints
- Wait for circuit breaker to reset (30 seconds)

### Rate Limit Exceeded (429)

**Cause**: Too many requests in a short time

**Solution**:
- Wait for rate limit to reset
- Check `Retry-After` header
- Increase rate limit in configuration if needed

### Unauthorized (401)

**Cause**: Missing or invalid JWT token

**Solution**:
- Login to get a valid token
- Check token expiration
- Include Authorization header: `Bearer <token>`

### Service Connection Errors

**Cause**: Cannot connect to downstream services

**Solution**:
- Check service URLs in configuration
- Ensure services are running
- Check firewall and network settings

## Development

### Adding a New Service

1. Add HTTP client configuration in `Configuration/HttpClientConfiguration.cs`
2. Create proxy controller in `Controllers/`
3. Update `appsettings.json` with service URL
4. Add routes to Swagger documentation

### Testing

```bash
# Run tests
dotnet test

# Run with specific service configuration
dotnet run --urls=http://localhost:8092
```

## Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8092

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Binah.API.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Binah.API.dll"]
```

```bash
# Build Docker image
docker build -t binah-api-gateway .

# Run container
docker run -p 8092:8092 \
  -e Authentication__Jwt__SecretKey="your-secret-key" \
  -e Services__Ontology__Url="http://ontology:5001" \
  binah-api-gateway
```

## License

Part of the Binah platform.
