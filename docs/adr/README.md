# Architecture Decision Records (ADRs)

Este directorio contiene las decisiones arquitectónicas importantes del proyecto, cada una documentada con su contexto, alternativas consideradas, decisión tomada y consecuencias aceptadas.

## ¿Qué es un ADR?

Un **Architecture Decision Record** es un documento corto (1-3 páginas) que captura una decisión técnica relevante en el momento en que se toma. Sirve para:

- Entender *por qué* el código es como es, meses o años después.
- Evitar re-debatir decisiones ya tomadas ("¿por qué usamos PostgreSQL y no Mongo?").
- Detectar cuándo una decisión ya no aplica porque el contexto cambió.
- Onboarding rápido de nuevos colaboradores.

El valor de un ADR está en escribirlo **cuando la decisión se toma**, no retroactivamente.

## Cuándo escribir un ADR

Escribir un ADR cuando la decisión:

- **Es difícil de revertir** (cambiar lenguaje, base de datos, patrón arquitectónico, proveedor crítico).
- **Tiene consecuencias visibles en el código durante mucho tiempo** (patrones como CQRS, outbox, anticorruption layer).
- **Involucra trade-offs significativos** donde otras personas podrían cuestionar la elección.
- **Afecta a múltiples módulos o equipos**.

**No escribir un ADR** para decisiones reversibles de bajo impacto, estilos de código (eso va en el linter), o features de producto (eso va en backlog).

## Formato

Todos los ADRs siguen el template en `000-template.md`. Estructura:

1. Metadata (fecha, estado, decididores).
2. Contexto y problema.
3. Fuerzas en tensión.
4. Alternativas consideradas.
5. Decisión.
6. Consecuencias (positivas, negativas, neutrales).
7. Cuándo reevaluar.
8. Referencias.

## Estados posibles

- **Propuesto:** en discusión, todavía no implementado.
- **Aceptado:** decidido e implementado (o en implementación).
- **Deprecado:** ya no aplica pero se conserva por historia.
- **Reemplazado por ADR-NNNN:** superado por una decisión posterior (link al nuevo).

## Convenciones de nombrado

`NNNN-titulo-kebab-case.md`

- `NNNN` = número secuencial con padding de ceros (0001, 0002, ..., 0123).
- Título corto, descriptivo, en español, kebab-case.
- Nunca reutilizar números, aunque un ADR sea deprecado.

Ejemplos: `0001-monolito-modular.md`, `0007-postgresql-como-db-principal.md`.

## Índice de ADRs

### Aceptados

| # | Título | Fecha | Estado |
|---|--------|-------|--------|
| [0001](./0001-monolito-modular.md) | Monolito modular sobre microservicios | 2026-04-22 | Aceptado |

### Propuestos (en discusión)

_(ninguno todavía)_

### Deprecados

_(ninguno todavía)_

## ADRs previstos a escribir

Lista de decisiones importantes que van a tomarse y documentarse a medida que el proyecto avance. **No escribir estos preventivamente**: escribirlos cuando la decisión se tome realmente, con contexto real.

### Fase de fundaciones técnicas

- **ADR-0002:** Clean Architecture con nomenclatura estándar + mapping a Mousqués.
- **ADR-0003:** PostgreSQL + TimescaleDB como stack de datos principal.
- **ADR-0004:** Multi-tenancy por fila con Row Level Security.
- **ADR-0005:** Outbox pattern para eventos de integración.
- **ADR-0006:** Strongly-typed IDs en el dominio.
- **ADR-0007:** Autenticación: ASP.NET Core Identity vs. Clerk vs. Auth0 (a decidir).
- **ADR-0008:** Estrategia de testing (MSTest + Testcontainers + cobertura por capa).

### Fase de ingesta y procesamiento

- **ADR-0009:** Anticorruption Layer por proveedor externo (Strava, Garmin, etc.).
- **ADR-0010:** Manejo de tokens OAuth (cifrado a nivel aplicación + rotación).
- **ADR-0011:** TimescaleDB vs. almacenamiento columnar para streams de actividad.
- **ADR-0012:** Estrategia de reintentos y dead letter queue para sincronizaciones.

### Fase de inteligencia

- **ADR-0013:** Abstracción `IInsightGenerator` para desacoplar del proveedor LLM.
- **ADR-0014:** Structured outputs y validación de respuestas de LLM.
- **ADR-0015:** Versionado de prompts como artefacto de código.
- **ADR-0016:** Coach-in-the-loop como requisito no negociable.
- **ADR-0017:** Caching de respuestas de LLM para control de costos.

### Fase de producto

- **ADR-0018:** PWA sobre React Native para la app del atleta (MVP).
- **ADR-0019:** Monorepo con pnpm + Turborepo para frontends.
- **ADR-0020:** Generación de cliente API tipado desde OpenAPI.
- **ADR-0021:** BFF pattern para separar concerns de web (coach) y mobile (atleta).

### Fase de infraestructura y operación

- **ADR-0022:** Railway/Fly.io como PaaS inicial, migración a AWS/Azure como siguiente paso.
- **ADR-0023:** GitHub Actions para CI/CD con gates manuales a producción.
- **ADR-0024:** Gitflow como estrategia de branching.
- **ADR-0025:** Conventional Commits en español.
- **ADR-0026:** Stack de observabilidad (Serilog + OpenTelemetry + Sentry).
- **ADR-0027:** Estrategia de backups y test de restore.

### Fase comercial

- **ADR-0028:** Stripe + MercadoPago como gateway de pagos.
- **ADR-0029:** Modelo de pricing por tiers basados en atletas gestionados.
- **ADR-0030:** Data retention policy y compliance con ley 18.331 / GDPR.

Esta lista es orientativa y va a cambiar a medida que el proyecto evolucione. Algunos ADRs previstos pueden no llegar a escribirse porque la decisión se resuelve trivialmente; otros aparecerán porque surgieron problemas no anticipados.

## Proceso para escribir un ADR nuevo

1. Copiar `000-template.md` a `NNNN-titulo.md` con el siguiente número disponible.
2. Completar secciones en orden: contexto → alternativas → decisión → consecuencias.
3. Marcar estado inicial como `Propuesto` si necesita discusión, `Aceptado` si ya está decidido.
4. Agregar al índice de este README.
5. Commitear con mensaje `docs(adr): agregar ADR-NNNN sobre <tema>`.
6. Si la decisión afecta a `CLAUDE.md` o `docs/ARCHITECTURE.md`, actualizar esos archivos en el mismo PR.

## Cuándo deprecar un ADR

Si una decisión documentada ya no aplica:

1. Cambiar estado a `Deprecado` o `Reemplazado por ADR-NNNN`.
2. Agregar sección al final explicando por qué.
3. Si fue reemplazado, linkear al nuevo ADR (bidireccionalmente).
4. **No borrar el ADR viejo.** La historia importa.

---

*Los ADRs son contratos con el futuro. Tomalos en serio, pero no dejes que se vuelvan burocracia. El objetivo es claridad, no documentación defensiva.*
