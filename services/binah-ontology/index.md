# Binah Ontology Service - API Documentation

Welcome to the Binah Ontology Service API documentation.

## Overview

The Binah Ontology Service manages domain ontologies, entity definitions, and relationship mappings for the Binelek platform. It provides a centralized ontology registry that powers dynamic entity generation and domain-driven design across all microservices.

## Key Features

- **Ontology Version Management** - Upload, publish, and rollback ontology versions
- **Entity Definition** - Define entities, properties, and relationships in YAML
- **GraphQL API** - Query ontology data using GraphQL
- **Neo4j Integration** - Store and query ontology graphs
- **Multi-tenant Support** - Isolated ontologies per tenant
- **Validation** - Comprehensive validation of ontology definitions

## API Reference

For detailed API documentation, see the [API Reference](api/index.md).

## Architecture

The service is built with:
- **.NET 8.0** - Modern C# web API
- **Neo4j** - Graph database for ontology storage
- **HotChocolate** - GraphQL server
- **Kafka** - Event streaming
- **PostgreSQL** - Ontology version metadata

## Getting Started

To get started with the Binah Ontology Service API, refer to the controller documentation:

- [OntologyController](api/Binah.Ontology.Controllers.OntologyController.yml) - Ontology version management endpoints
- [GraphQL API](api/Binah.Ontology.GraphQL.yml) - GraphQL queries and mutations

## Related Services

- **binah-api** - Main API gateway
- **binah-auth** - Authentication and authorization
- **binah-regen** - Code generation from ontologies

---

**Version:** 1.0.0
**Last Updated:** 2025-11-21
