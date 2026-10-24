CREATE TABLE IF NOT EXISTS ra_checkout_intents (
 account_id TEXT PRIMARY KEY REFERENCES ra_accounts(id) ON DELETE CASCADE,
 intent_id TEXT NOT NULL UNIQUE,request_id TEXT NOT NULL,price_id TEXT NOT NULL,
 provider_session_id TEXT,url TEXT,created_at INTEGER NOT NULL,expires_at INTEGER NOT NULL
);
