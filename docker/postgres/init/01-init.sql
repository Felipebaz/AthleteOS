-- ============================================================================
-- AthleteOS — Inicialización de base de datos
-- ============================================================================
--
-- Este script corre UNA SOLA VEZ, la primera vez que el volumen de PostgreSQL
-- está vacío. Si necesitás re-ejecutarlo, hacé `docker compose down -v` y
-- volvé a levantar. CUIDADO: eso borra todos los datos.
--
-- Responsabilidades de este script:
--   1. Habilitar extensiones necesarias (TimescaleDB, pgvector, pgcrypto).
--   2. Crear los schemas por bounded context.
--   3. Crear el rol de aplicación con permisos mínimos.
--
-- Las tablas NO se crean acá. Se crean via EF Core migrations cuando la app
-- arranca por primera vez (o cuando se corre `dotnet ef database update`).
--
-- ============================================================================


-- ----------------------------------------------------------------------------
-- Extensiones
-- ----------------------------------------------------------------------------

-- TimescaleDB viene preinstalada en la imagen que usamos, pero hay que
-- habilitarla explícitamente en la DB.
CREATE EXTENSION IF NOT EXISTS timescaledb;

-- pgvector para embeddings (búsqueda semántica en notas, similitud entre planes).
-- Uso real viene en fases posteriores, pero la habilitamos desde el inicio.
CREATE EXTENSION IF NOT EXISTS vector;

-- pgcrypto para funciones de cifrado a nivel DB (gen_random_uuid, digest, etc.).
-- Útil para IDs UUIDv4 del lado DB si hace falta, y para hashing auxiliar.
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- uuid-ossp: deprecada en favor de gen_random_uuid() de pgcrypto, pero algunas
-- herramientas legacy la esperan. La habilitamos por compatibilidad.
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";


-- ----------------------------------------------------------------------------
-- Schemas por bounded context
-- ----------------------------------------------------------------------------
-- Cada bounded context tiene su propio schema. Ver ARCHITECTURE.md nivel 5.
-- Las tablas de cada módulo viven solo en su schema.
-- Cross-schema joins están prohibidos por arquitectura.
-- ----------------------------------------------------------------------------

CREATE SCHEMA IF NOT EXISTS identity;
COMMENT ON SCHEMA identity IS 'Bounded context: Identity & Access. Usuarios, auth, tenants.';

CREATE SCHEMA IF NOT EXISTS athlete_profile;
COMMENT ON SCHEMA athlete_profile IS 'Bounded context: Athlete Profile. Perfil deportivo del atleta.';

CREATE SCHEMA IF NOT EXISTS training_data;
COMMENT ON SCHEMA training_data IS 'Bounded context: Training Data. Ingesta y normalización de wearables.';

CREATE SCHEMA IF NOT EXISTS coaching;
COMMENT ON SCHEMA coaching IS 'Bounded context: Coaching. Planes, sesiones, relación coach-atleta.';

CREATE SCHEMA IF NOT EXISTS intelligence;
COMMENT ON SCHEMA intelligence IS 'Bounded context: Intelligence. Análisis, sugerencias, IA.';

CREATE SCHEMA IF NOT EXISTS communication;
COMMENT ON SCHEMA communication IS 'Bounded context: Communication. Mensajería y notificaciones.';

-- Schema para Billing lo creamos ahora aunque se use en Fase 2.
CREATE SCHEMA IF NOT EXISTS billing;
COMMENT ON SCHEMA billing IS 'Bounded context: Billing. Suscripciones y pagos. (Fase 2)';

-- Schema para auditoría transversal.
CREATE SCHEMA IF NOT EXISTS audit;
COMMENT ON SCHEMA audit IS 'Audit log transversal, append-only. Retención 5 años.';


-- ----------------------------------------------------------------------------
-- Notas para futuras migrations (no ejecutar acá)
-- ----------------------------------------------------------------------------
-- Cuando se creen las primeras tablas en cada schema, recordar:
--
-- 1. Habilitar RLS (Row Level Security) en tablas con tenant_id:
--      ALTER TABLE coaching.training_plans ENABLE ROW LEVEL SECURITY;
--      CREATE POLICY tenant_isolation ON coaching.training_plans
--        USING (tenant_id = current_setting('app.current_tenant')::uuid);
--
-- 2. Convertir tablas de streams (ej. activity_samples) en hypertables de
--    TimescaleDB:
--      SELECT create_hypertable('training_data.activity_samples', 'timestamp');
--
-- 3. Crear tabla outbox en cada schema de módulo:
--      CREATE TABLE coaching.outbox_messages (
--        id UUID PRIMARY KEY,
--        event_type TEXT NOT NULL,
--        payload JSONB NOT NULL,
--        occurred_at TIMESTAMPTZ NOT NULL,
--        processed_at TIMESTAMPTZ NULL
--      );
-- ----------------------------------------------------------------------------


-- ----------------------------------------------------------------------------
-- Verificación
-- ----------------------------------------------------------------------------
-- Este bloque solo loguea qué se creó, para que el log de Docker sea útil.
-- ----------------------------------------------------------------------------

DO $$
BEGIN
    RAISE NOTICE '========================================';
    RAISE NOTICE 'AthleteOS database initialized';
    RAISE NOTICE 'Extensions: timescaledb, vector, pgcrypto, uuid-ossp';
    RAISE NOTICE 'Schemas: identity, athlete_profile, training_data,';
    RAISE NOTICE '         coaching, intelligence, communication,';
    RAISE NOTICE '         billing, audit';
    RAISE NOTICE '========================================';
END $$;
