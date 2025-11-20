-- Migration: Add Webhooks
-- Created: 2025-11-08
-- Description: Creates webhook_subscriptions and webhook_deliveries tables for webhook integration feature

CREATE TABLE IF NOT EXISTS webhook_subscriptions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id VARCHAR(255) NOT NULL,
    name VARCHAR(255) NOT NULL,
    url VARCHAR(500) NOT NULL,
    events JSONB NOT NULL DEFAULT '[]',
    secret VARCHAR(255) NOT NULL,
    active BOOLEAN DEFAULT true,
    headers JSONB DEFAULT '{}',
    retry_count INTEGER DEFAULT 3,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS webhook_deliveries (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    subscription_id UUID REFERENCES webhook_subscriptions(id) ON DELETE CASCADE,
    event_type VARCHAR(255) NOT NULL,
    payload JSONB NOT NULL,
    response_status VARCHAR(50),
    response_code INTEGER DEFAULT 0,
    response_body TEXT,
    attempt_number INTEGER DEFAULT 1,
    delivered_at TIMESTAMP DEFAULT NOW()
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_webhook_tenant ON webhook_subscriptions(tenant_id);
CREATE INDEX IF NOT EXISTS idx_webhook_deliveries_sub ON webhook_deliveries(subscription_id);
CREATE INDEX IF NOT EXISTS idx_webhook_deliveries_event ON webhook_deliveries(event_type);

-- Add comments to tables
COMMENT ON TABLE webhook_subscriptions IS 'Webhook subscriptions for event notifications';
COMMENT ON TABLE webhook_deliveries IS 'Webhook delivery history and logs';
