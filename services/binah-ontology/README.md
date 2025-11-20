# Binah Ontology Server

> **📚 For detailed technical documentation, see [docs/services/binah-ontology.md](../../docs/services/binah-ontology.md)**


## Features
- Maintain knowledge graph of all construction and real estate entities
- Provide CRUD operations for entities
- Manage relationships between entities (HAS_CONTRACTOR, INVESTS_IN, LOCATED_IN, etc.)
- Execute Cypher queries for complex graph traversals
- Track entity versioning and lineage
- Emit events on graph mutations via Kafka
- Support real-time subscriptions via GraphQL subscriptions

## Old Architecture
```mermaid
flowchart LR
  subgraph "EDGE / INFRA"
    API_GW["Gateway API (8092) - Rate Limit / AuthN/Z"]
    Auth[Auth Server - OAuth2 / JWT / Introspection]
    CDN[CDN / Static Assets]
  end

  subgraph "INGESTION & CONTEXT"
    APIConn[API Connectors Service]
    FileProc[File Processors Service]
    Queue[Kafka Cluster]
    Context["Context Server (.NET 8) - Orchestration"]
  end

  subgraph "PLATFORM SERVICES"
    Ontology["Ontology Server (.NET 8) - 8088"]
    AI_Srv["AI Server (Python 3.11) - 8090"]
    LLM_Srv["LLM Server (Python 3.11) - 8091 - A2A Enabled"]
    AuthZ[AuthZ Policy Engine]
  end

  subgraph "STORAGE & INDEX"
    Neo4j[(Neo4j 5.15 - Knowledge Graph)]
    Qdrant[(Qdrant 1.7 - Vector Store)]
    Postgres[(PostgreSQL 16 - Canonical Tables)]
    DW[(Snowflake / Data Warehouse)]
    S3[(Object Store - S3/GCS)]
    ES[(Elasticsearch - Fulltext/Geo)]
    Redis[(Redis 7.2 - Cache)]
  end

  subgraph "ML / MLOps"
    MLflow[(MLflow 2.10 - Model Registry)]
    Training[Model Training / Batch Scoring]
  end

  subgraph "OBSERVABILITY"
    Prom[Prometheus]
    Graf[Grafana]
    Logs[Serilog -> ELK/Cloud Logging]
  end

  %% Data flows
  MLS[MLS / Listing Providers] -->|API| APIConn
  Zillow[Commercial Feeds] -->|API| APIConn
  County[County Deeds & Parcels] -->|Files/APIs| FileProc
  GIS[GIS Systems / Shapefiles] -->|Files| FileProc
  Permits[Permit Systems] -->|Files/API| FileProc
  Financial[Loans/Bank Feeds] -->|API| APIConn
  Env[Environmental Feeds] -->|API| APIConn

  APIConn & FileProc -->|produce| Queue
  Queue -->|consume| Context
  Context -->|validate/normalize| Ontology
  Context -->|upsert| Neo4j
  Context -->|store canon| Postgres
  Context -->|store files| S3
  Context -->|index| ES
  Context -->|embeddings| Qdrant
  Ontology --> Neo4j
  AI_Srv -->|model training/score| MLflow
  Training --> MLflow
  Neo4j --> AI_Srv
  Neo4j --> LLM_Srv
  Qdrant --> LLM_Srv
  Postgres --> MLflow
  ES --> AI_Srv

  API_GW -->|JWT/OAuth2| Auth
  API_GW --> Context
  API_GW --> Ontology
  API_GW --> Neo4j
  API_GW --> ES
  API_GW --> MLflow
  API_GW --> LLM_Srv

  %% A2A path (LLM to LLM)
  ExternalLLM["Third-party LLM (A2A Client)"] -->|mutual-TLS + OAuth2 Client Credentials| API_GW
  API_GW -->|"forward A2A request (mTLS/verified)"| LLM_Srv
  LLM_Srv -->|introspect token| Auth

  %% Observability
  Context & Ontology & AI_Srv & LLM_Srv --> Prom
  Prom --> Graf
  Context & Ontology --> Logs

```
