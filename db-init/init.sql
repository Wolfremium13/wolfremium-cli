-- Initialize Wolfremium Dev Database

-- Create Users table
CREATE TABLE IF NOT EXISTS "users" (
    "id" SERIAL PRIMARY KEY,
    "username" VARCHAR(255) NOT NULL,
    "email" VARCHAR(255) NOT NULL,
    "is_active" BOOLEAN DEFAULT TRUE,
    "created_at" TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Create Tasks table
CREATE TABLE IF NOT EXISTS "tasks" (
    "id" SERIAL PRIMARY KEY,
    "title" VARCHAR(255) NOT NULL,
    "description" VARCHAR(255),
    "user_id" INTEGER,
    "due_date" TIMESTAMP WITHOUT TIME ZONE
);

-- Mock Data for Users
INSERT INTO "users" ("username", "email", "is_active", "created_at") VALUES
('wolfremium_admin', 'admin@wolfremium.dev', TRUE, '2026-01-01 10:00:00'::timestamp),
('developer_jane', 'jane@wolfremium.dev', TRUE, '2026-02-15 14:30:00'::timestamp),
('legacy_tester', 'tester@wolfremium.dev', FALSE, '2025-11-20 09:15:00'::timestamp);

-- Mock Data for Tasks
INSERT INTO "tasks" ("title", "description", "user_id", "due_date") VALUES
('Setup Docker Environment', 'Configure postgres and CLI service containers', 1, '2026-06-10 18:00:00'::timestamp),
('Fix Index Overflow', 'Resolve the negative spinner frames index error', 2, '2026-06-03 12:00:00'::timestamp),
('Implement Unit Tests', 'Add code coverage for backup workflows', 2, '2026-06-20 17:00:00'::timestamp);
