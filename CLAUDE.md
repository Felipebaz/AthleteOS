# CLAUDE.md

> Briefing para agentes de IA (Claude Code) trabajando en este repositorio.
> Los humanos leen el [README](./README.md). Vos leés esto.

---

## Regla de oro

**Antes de escribir código, leé `docs/ARCHITECTURE.md`.** Ahí están los 15 niveles de arquitectura del proyecto: problema, requisitos, bounded contexts, agregados, flujos, stack, roadmap. Todo lo que necesitás para trabajar con criterio está ahí. Este archivo es el mapa hacia ese documento y las reglas operativas que lo complementan.

Si te pido algo que contradice `docs/ARCHITECTURE.md`, **preguntá antes de actuar**. El documento de arquitectura es la fuente de verdad; este archivo opera dentro de sus reglas.

---

## Qué es este proyecto en 3 líneas

Plataforma SaaS B2B para coaches de deportes de endurance (running, ciclismo, triatlón, natación) que gestionan atletas a distancia. Ingiere datos de wearables (Strava, Garmin, Polar), analiza con IA, y presenta al coach un dashboard priorizado con sugerencias accionables que el coach aprueba. El usuario pagante es el coach, no el atleta.

**Nombre actual:** placeholder (`AthleteOS` / `CoachLens`). No es definitivo. No dedicarle tiempo.

**Fase actual:** `[ACTUALIZAR: Fase 0 - Validación | Fase 1 - Fundaciones | Fase 2 - Ingesta | etc.]`

---

## Stack en una pantalla

```
Backend:     C# / .NET 8 + ASP.NET Core Minimal APIs + EF Core 8 + MediatR
             FluentValidation + Serilog + OpenTelemetry + Hangfire + MSTest
Data:        PostgreSQL 16 + TimescaleDB + pgvector + Redis
Frontend:    React 18 + TypeScript + Vite + TanStack (Query/Router) + Zustand
             Tailwind + shadcn/ui + Vitest + Playwright
IA:          Anthropic Claude via API (structured outputs)
Monorepo:    pnpm workspaces + Turborepo (frontends). Backend en repo separado o carpeta.
Infra MVP:   Railway o Fly.io + Cloudflare R2 (storage) + Cloudflare CDN
CI/CD:       GitHub Actions + Docker
Testing:     MSTest + FluentAssertions + Testcontainers + Bogus + NSubstitute
```

Detalle completo y justificaciones en `docs/ARCHITECTURE.md` nivel 13.

---

## Bounded contexts (qué partes hay)

El sistema es un **monolito modular** organizado por bounded contexts (DDD). Cada uno tiene su dominio, su schema de DB y sus endpoints aislados.

| Contexto | Tipo | Qué hace |
|----------|------|---------|
| `Identity` | Generic | Auth, usuarios, tenants, invitaciones |
| `AthleteProfile` | Supporting | Perfil deportivo, zonas, objetivos, lesiones |
| `TrainingData` | Supporting | Ingesta y normalización de datos externos |
| `Coaching` | **Core** | Planes, ejecución, relación coach-atleta |
| `Intelligence` | **Core** | Readiness, sugerencias, aprendizaje |
| `Communication` | Supporting | Mensajería, notificaciones |
| `Billing` | Generic | Suscripciones, pagos (fase 2) |

**Regla fundamental:** los módulos NO se referencian entre sí por código. Se comunican por:
1. **Integration events** (asincrónicos, vía outbox + bus).
2. **Public API de módulo** (interfaces expuestas en `ModuleX.Contracts`).

Si estás tentado a hacer `using Coaching.Domain` desde `Intelligence.Application`, **parálo y pedí orientación**. Probablemente necesitás un evento o un contract.

---

## Reglas de arquitectura no negociables

Estas son las reglas que mantienen el proyecto sano. Violarlas causa dolor técnico a futuro y se rechaza en code review.

### 1. Clean Architecture estricta

Dependencias apuntan hacia el dominio:

```
Api ──► Application ──► Domain
 │           │
 └──► Infrastructure ──► Application ──► Domain
```

- `Domain` no depende de nada (ni siquiera de `Microsoft.*` fuera de `System.*`).
- `Application` depende solo de `Domain` (y de `BuildingBlocks.Application`).
- `Infrastructure` implementa interfaces declaradas en `Domain` o `Application`.
- `Api` compone todo.

**Verificación:** si estás escribiendo una clase `EntityFrameworkRepository` dentro de `Domain`, estás mal. Si en `Domain` hay un `using Microsoft.EntityFrameworkCore`, estás mal.

### 2. Un caso de uso = un agregado

Un command handler modifica **un solo agregado**. Si parece que necesitás modificar dos, reconsiderá:
- ¿Son realmente el mismo agregado?
- ¿Debería ser eventual consistency con un evento?
- ¿Falta un domain service?

Nunca hagas transacciones que toquen 2 agregados distintos con el mismo `SaveChanges`.

### 3. Outbox pattern para eventos de integración

Eventos cross-context **siempre** pasan por outbox:
1. Command handler modifica agregado.
2. En la misma transacción, se inserta evento en tabla `outbox`.
3. Worker `OutboxPublisher` lee outbox y publica al bus.
4. Consumidores registran en tabla `inbox` para idempotencia.

Nunca publiques al bus directamente desde un handler. Nunca.

### 4. Strongly-typed IDs

No usar `Guid` crudo como identificador en dominio. Siempre wrappers tipados:

```csharp
public readonly record struct AthleteId(Guid Value);
public readonly record struct TrainingPlanId(Guid Value);
```

EF Core convierte automáticamente con `ValueConverter` configurado en `BuildingBlocks.Infrastructure`.

### 5. Tenant isolation

Cada tabla de negocio tiene `tenant_id` NOT NULL. Nunca lo omitas en:
- Creación de entidades.
- Queries (siempre filtrar).
- Migrations.

Row Level Security (RLS) de PostgreSQL está habilitado. Si tu query bypass RLS, el test fallará.

### 6. No LLM en el dominio

Las llamadas a Anthropic viven **solo en `Intelligence.Infrastructure.Llm`**. El dominio de Intelligence no sabe qué proveedor está abajo. Depende de `IInsightGenerator`.

Misma regla para cualquier servicio externo: adapter en Infrastructure, interface en Application o Domain.

### 7. Eventos de dominio != eventos de integración

- **Domain events:** in-process, dentro del mismo bounded context, despachados por MediatR dentro de la unidad de trabajo. Sufijo: `DomainEvent`.
- **Integration events:** cross-context, asincrónicos, via outbox. Sufijo: `IntegrationEvent`.

No los confundas. Un `TrainingPlanCreatedDomainEvent` puede disparar lógica interna del módulo Coaching. Un `TrainingPlanCreatedIntegrationEvent` puede ser consumido por Communication para notificar al atleta.

### 8. Result pattern, no excepciones para flujo

Errores esperables (validación, recurso no encontrado, regla de negocio violada) se devuelven como `Result<T>` o `Result`. Excepciones solo para errores realmente excepcionales (bug, infraestructura caída).

```csharp
// Bien
return Result.Failure<AthleteDto>(AthleteErrors.NotFound);

// Mal (para errores esperables)
throw new NotFoundException("Athlete not found");
```

Invariantes violadas en agregados sí lanzan excepciones del dominio (`InvalidPlanAdjustmentException`), porque representan un bug del caller.

### 9. Tests son parte del Definition of Done

- Cobertura mínima: Domain 90%+, Application 70%+, global 60%+.
- Todo caso de uso nuevo necesita test.
- Todo agregado nuevo necesita tests de invariantes.
- Tests de integración con Testcontainers (PostgreSQL real, no in-memory).
- No se mergea si CI está rojo.

### 10. Observabilidad desde el inicio

Código nuevo debe tener:
- Logs estructurados con Serilog (nivel apropiado, no `Console.WriteLine`).
- Métricas OpenTelemetry si es operación relevante.
- Manejo de errores que preserve contexto (Sentry con tags útiles).

---

## Estructura de carpetas del backend

```
src/
├── BuildingBlocks/
│   ├── BuildingBlocks.Domain/            # Entity, AggregateRoot, ValueObject, DomainEvent base
│   ├── BuildingBlocks.Application/        # MediatR behaviors, abstractions
│   ├── BuildingBlocks.Infrastructure/     # Outbox, interceptors, event bus
│   └── BuildingBlocks.Api/                # Middlewares, filters
│
├── Modules/
│   ├── {ModuleName}/
│   │   ├── {ModuleName}.Domain/
│   │   ├── {ModuleName}.Application/
│   │   ├── {ModuleName}.Infrastructure/
│   │   ├── {ModuleName}.Api/
│   │   └── {ModuleName}.Contracts/        # public API para otros módulos (opcional)
│   └── ...
│
├── Bootstrap/
│   └── ApiHost/                           # Program.cs, compone módulos
│
└── Workers/
    ├── SyncWorker/
    ├── AnalysisWorker/
    ├── AiWorker/
    ├── NotificationWorker/
    └── OutboxPublisher/

tests/
├── Modules/
│   ├── {ModuleName}.UnitTests/
│   └── {ModuleName}.IntegrationTests/
└── E2E/
```

**Al crear un módulo nuevo:** seguir la estructura interna detallada en `docs/ARCHITECTURE.md` nivel 13.1.

---

## Convenciones de código

### Nombres

- **Clases, records, structs:** `PascalCase`.
- **Métodos, propiedades:** `PascalCase`.
- **Variables locales, parámetros:** `camelCase`.
- **Campos privados:** `_camelCase` con underscore.
- **Constantes:** `PascalCase` (no SCREAMING_SNAKE).
- **Archivos:** un tipo público por archivo, nombre == tipo.

### Nombres de dominio

- **Sí:** `TrainingPlan`, `Athlete`, `CoachSuggestion`, `ReadinessSnapshot`, `ApplyCoachSuggestionCommand`.
- **No:** `UserManager`, `DataHelper`, `ProcessorService`, `UtilClass`.

Los nombres reflejan el **lenguaje ubicuo del negocio**, no patrones técnicos.

### Idioma

- **Código (clases, variables, métodos):** inglés. Siempre.
- **Comentarios:** español o inglés, consistente en el archivo.
- **Commits:** Conventional Commits en español.
- **Docs (README, ADRs, este archivo):** español.
- **Mensajes de log:** inglés (más fácil de buscar).
- **Strings visibles al usuario:** español (i18n preparada).

### Commits (Conventional Commits en español)

Formato: `<tipo>(<scope>): <descripción corta>`

Tipos usados: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `perf`, `build`, `ci`.

Ejemplos:

```
feat(coaching): agregar ajuste de semana con versionado
fix(training-data): corregir matcheo de actividad con sesión planificada
refactor(intelligence): extraer cálculo de readiness a domain service
test(coaching): cubrir invariantes de progresión de carga
docs(adr): agregar ADR sobre elección de outbox pattern
chore(deps): actualizar MediatR a 12.2.0
```

Scope = módulo o área (`coaching`, `intelligence`, `infra`, `ci`, etc.).

### Branches

Gitflow:
- `main` — producción.
- `develop` — integración continua, ambiente `dev`.
- `feature/<nombre-descriptivo>` — features nuevas.
- `fix/<nombre>` — bugfixes.
- `hotfix/<nombre>` — parches urgentes desde main.
- `release/<version>` — estabilización pre-prod.

**Nunca commitear directo a `main` ni a `develop`.** Siempre PR.

---

## Cuando te pido implementar algo nuevo

Seguí esta checklist mental:

1. **¿Está en `docs/ARCHITECTURE.md`?** Referenciá la sección. Si lo que pido contradice el doc, preguntá primero.
2. **¿A qué bounded context pertenece?** Si toca varios, hay que pensar en eventos.
3. **¿Es command o query?** Command = pasa por agregado. Query = lectura directa con DTO.
4. **¿Qué agregado modifica?** Solo uno.
5. **¿Qué eventos emite?** ¿Domain event o integration event?
6. **¿Qué invariantes tiene que preservar?**
7. **¿Qué tests necesita?** Unit del agregado + unit del handler + integration si hay repo nuevo.
8. **¿Requiere cambio en DB?** Migration reversible. Nunca tocar migrations ya aplicadas en prod.
9. **¿Afecta la API pública?** Actualizar contratos, versionar si es breaking.
10. **¿Toca secretos o datos sensibles?** Extra cuidado con cifrado, logs, auditoría.

Al terminar: **¿actualicé docs si cambió arquitectura? ¿agregué ADR si fue decisión importante?**

---

## Cuando te pido algo y no sé bien qué quiero

Si la solicitud es ambigua o demasiado amplia, **no implementes a ciegas**. Respondé con:

1. Tu interpretación de lo que pedí.
2. 2-3 preguntas concretas de clarificación.
3. Una propuesta de enfoque.

Ejemplo de mala respuesta: implementar 4 archivos y 200 líneas ante "agregá la feature de notificaciones".

Ejemplo de buena respuesta: "Entiendo que querés notificaciones cuando el plan se ajusta. Antes de implementar: (a) ¿web push, email, ambos? (b) ¿solo atleta o también coach? (c) ¿qué eventos gatillan notificaciones?"

---

## Lo que NO debés hacer (ni aunque te lo pida)

- **No instalar paquetes sin preguntar.** Dependencias nuevas requieren evaluación.
- **No migrar a otro ORM/DB/framework por impulso.** Cambios de stack son ADRs.
- **No borrar tests.** Si un test falla, se arregla el código o se entiende el test. Borrarlo es regresión silenciosa.
- **No commitear secretos.** Ni siquiera en branches de feature. Ni "temporalmente".
- **No desactivar checks de CI para hacer merge.** Si CI falla, se arregla.
- **No introducir microservicios.** Monolito modular hasta que haya razón de negocio para partir. Decisión está en ADR-001.
- **No escribir código sin tests** en módulos de dominio (core).
- **No optimizar prematuramente.** Medí primero. Optimizá después.
- **No reescribir código que funciona** "porque se ve feo", salvo que haya motivo claro.
- **No generar migrations destructivas** sin confirmación explícita del usuario.

---

## Datos sensibles y seguridad

Este producto maneja **datos de salud** (HRV, sueño, peso, lesiones). Categoría especial bajo GDPR/LGPD/ley 18.331 de Uruguay.

### Tratamiento obligatorio

- Tokens OAuth de wearables (Garmin, Strava): **cifrados a nivel aplicación** antes de persistir.
- Datos médicos/lesiones: cifrados en reposo, nunca en logs.
- PII (emails, nombres): nunca en logs de error con detalles.
- Mensajes coach-atleta: nunca en logs, nunca enviados a LLM sin redacción si contienen datos identificables.

### Logs seguros

```csharp
// Mal
_logger.LogInformation("Sync for athlete {Email} starting", athlete.Email);

// Bien
_logger.LogInformation("Sync for athlete {AthleteId} starting", athlete.Id);
```

PII solo en audit log dedicado, no en logs operacionales.

### Acceso a datos sensibles

Cualquier lectura por parte de admin/staff de datos de coach o atleta se loguea en audit log. No hay "ojear" sin registro.

---

## Trabajando con IA (Claude en el producto)

El sistema usa Claude de Anthropic para:
- Generación de sugerencias al coach.
- Generación de planes draft.
- Explicaciones narrativas de análisis.

### Reglas

1. **Nunca en el dominio.** Siempre detrás de `IInsightGenerator` u otro puerto.
2. **Siempre structured outputs.** Nunca parsear texto libre para decisiones de negocio. Pedí JSON con schema, validá con FluentValidation.
3. **Prompts versionados.** Viven en archivos (`Intelligence/Infrastructure/Llm/Prompts/v1/weekly-review.md`). Bump de versión al cambiar.
4. **Logging completo.** Cada llamada: prompt enviado, respuesta recibida, versión del prompt, latencia, tokens, costo estimado.
5. **Caching agresivo.** Respuestas idénticas a contextos idénticos no se vuelven a calcular (clave = hash del prompt + contexto).
6. **Coach-in-the-loop siempre.** Ninguna sugerencia se aplica sin aprobación humana. Ninguna.
7. **Reglas duras del dominio no se violan por sugerencia IA.** Si el LLM sugiere algo que viola progresión de carga, la capa de aplicación lo rechaza antes de llegar al coach.

---

## Comandos útiles (actualizar a medida que se creen)

```bash
# Backend .NET
dotnet build                            # build de toda la solución
dotnet test                             # todos los tests
dotnet test --filter Category=Unit      # solo unit tests
dotnet ef migrations add <Name> --project src/Modules/<Module>/<Module>.Infrastructure
dotnet ef database update --project src/Modules/<Module>/<Module>.Infrastructure

# Frontend
pnpm install                            # instala deps del monorepo
pnpm dev                                # arranca ambiente dev
pnpm test                               # tests
pnpm build                              # build de producción
pnpm lint                               # lint

# Docker local
docker compose up -d                    # levanta PostgreSQL, Redis, etc.
docker compose down                     # detiene todo
docker compose logs -f <servicio>       # logs
```

---

## Dónde está cada cosa

| Necesitás... | Andá a... |
|--------------|-----------|
| Visión del producto y problema | `docs/ARCHITECTURE.md` nivel 1 |
| Requisitos funcionales (IDs tipo RF-*) | `docs/ARCHITECTURE.md` nivel 2 |
| Requisitos no funcionales con metas | `docs/ARCHITECTURE.md` nivel 3 |
| Principios que guían decisiones | `docs/ARCHITECTURE.md` nivel 4 |
| Qué bounded contexts hay | `docs/ARCHITECTURE.md` nivel 5 |
| Vista de despliegue y componentes | `docs/ARCHITECTURE.md` nivel 6 |
| Modelo de datos y multi-tenancy | `docs/ARCHITECTURE.md` nivel 7 |
| Modelo de amenazas y defensa | `docs/ARCHITECTURE.md` nivel 8 |
| Riesgos técnicos y mitigaciones | `docs/ARCHITECTURE.md` nivel 9 |
| Estructura de módulos del backend | `docs/ARCHITECTURE.md` nivel 10 |
| Agregados, eventos, casos de uso por contexto | `docs/ARCHITECTURE.md` nivel 11 |
| Flujos end-to-end paso a paso | `docs/ARCHITECTURE.md` nivel 12 |
| Stack tecnológico completo con justificación | `docs/ARCHITECTURE.md` nivel 13 |
| CI/CD, ambientes, infra | `docs/ARCHITECTURE.md` nivel 14 |
| Roadmap y fases | `docs/ARCHITECTURE.md` nivel 15 |
| Decisiones arquitectónicas individuales | `docs/adr/` |
| Glosario del dominio | `docs/ARCHITECTURE.md` Apéndice A |

---

## Contacto con el humano

Soy **Felipe**. Estudiante de ingeniería de software en ORT (Uruguay), con experiencia en frontend (React/TypeScript) expandiendo a backend (C#/.NET). Este es mi proyecto de portfolio ambicioso + posible spin-off comercial.

### Preferencias cuando trabajás conmigo

- **Directo y técnico.** Nada de relleno, nada de "¡qué gran idea!". Al punto.
- **Honestidad ante malas decisiones.** Si lo que pido es malo, decímelo con razones. Prefiero pushback fundamentado que obediencia.
- **TDD cuando aplica.** Sobre todo en Domain y Application. Red-green-refactor.
- **Conventional Commits en español.**
- **Clean Architecture con la nomenclatura Mousqués** (curso ORT) mapeada a nombres estándar: `InterfazUsuario` = Api, `ServiciosAplicacion` = Application, `ReglasNegocio` = Domain, `AccesoDatos` = Infrastructure. En código uso nombres estándar en inglés; en comentarios/docs cualquiera de los dos sirve.
- **Español rioplatense** en docs y comunicación.

### Cómo respondo bien a vos

- Antes de escribir código complejo, proponeme el enfoque.
- Si dudás entre dos caminos, mostrámelos y recomendá uno.
- Explicá el "por qué" de decisiones no obvias.
- Marcá explícitamente cuándo estás haciendo algo fuera de lo pedido ("agregué X porque Y").

---

## Actualización de este documento

Este archivo evoluciona. Cuando cambie:
- Stack → actualizar sección Stack.
- Reglas de arquitectura → actualizar con referencia a ADR.
- Fase del proyecto → actualizar arriba.
- Comandos útiles → agregar a medida que se creen.

Si vas a actualizar este archivo, avisame antes. Es un contrato.

---

*Última actualización: [fecha]. Si encontrás contradicción entre este archivo y `docs/ARCHITECTURE.md`, el doc de arquitectura gana. Avisame para resolver la inconsistencia.*
