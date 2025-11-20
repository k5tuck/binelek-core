-- Migration: Add Audit Logging
-- Created: 2025-11-08
-- Description: Creates audit_logs table for enterprise audit logging feature

CREATE TABLE IF NOT EXISTS audit_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id VARCHAR(255) NOT NULL,
    user_id VARCHAR(255),
    action VARCHAR(255) NOT NULL,
    resource VARCHAR(255),
    resource_id VARCHAR(255),
    ip_address VARCHAR(45),
    user_agent TEXT,
    request_path VARCHAR(500),
    http_method VARCHAR(10),
    status_code INTEGER NOT NULL DEFAULT 0,
    details JSONB,
    timestamp TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_audit_tenant_timestamp ON audit_logs(tenant_id, timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_audit_user ON audit_logs(user_id);
CREATE INDEX IF NOT EXISTS idx_audit_resource ON audit_logs(resource, resource_id);
CREATE INDEX IF NOT EXISTS idx_audit_action ON audit_logs(action);

-- Add comment to table
COMMENT ON TABLE audit_logs IS 'Audit log entries for compliance and security tracking';
