-- Migration: Add SAML SSO Configuration
-- Created: 2025-11-08
-- Description: Creates saml_configurations table for enterprise SSO feature

CREATE TABLE IF NOT EXISTS saml_configurations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id VARCHAR(255) UNIQUE NOT NULL,
    entity_id VARCHAR(500) NOT NULL,
    sso_url VARCHAR(500) NOT NULL,
    x509_certificate TEXT NOT NULL,
    enabled BOOLEAN DEFAULT true,
    attribute_mapping JSONB DEFAULT '{}',
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- Add comment to table
COMMENT ON TABLE saml_configurations IS 'SAML SSO configuration for enterprise tenants';
