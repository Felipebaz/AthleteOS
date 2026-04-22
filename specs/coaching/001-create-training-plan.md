# SPEC 001: Crear plan de entrenamiento

---

## Metadata

- **ID:** SPEC-COACHING-001
- **Estado:** Accepted
- **Autor:** Felipe
- **Fecha creación:** 2026-04-22
- **Última actualización:** 2026-04-22
- **Issues/PRs relacionados:** —

---

## Contexto de dominio

- **Bounded context:** Coaching
- **Agregado(s) afectado(s):** `TrainingPlan` (creación)
- **Capacidades de negocio cubiertas:** RF-PLAN-01, RF-PLAN-02, RF-PLAN-03 (ver `docs/ARCHITECTURE.md` nivel 2.4)
- **Flujo end-to-end relacionado:** es precondición para los flujos del nivel 12 del ARCHITECTURE

---

## Historia de usuario

Como coach, quiero crear un plan de entrenamiento para un atleta mío, con un rango de fechas, un objetivo y una estructura semanal de sesiones, para que el atleta sepa qué debe entrenar cada día y yo pueda hacer seguimiento de su progreso.

---

## Invariantes preservadas

Todas estas invariantes del agregado `TrainingPlan` se validan al crear el plan:

1. **Rango de fechas válido:** `startDate < endDate` y `endDate > today - 7 días` (no se crean planes completamente en el pasado).
2. **Duración mínima de 1 semana y máxima de 52 semanas.** Planes más largos se dividen en mesociclos separados.
3. **Cobertura semanal completa:** las `TrainingWeek`s cubren todo el rango `[startDate, endDate]` sin huecos ni superposiciones.
4. **Cada `TrainingWeek` empieza lunes y termina domingo** (convención del dominio; ver nota de implementación).
5. **Al menos un objetivo (`Goal`) asociado** al crearse (referencia al objetivo activo del atleta).
6. **El coach creador es dueño del plan:** `CoachId` del plan == coach autenticado que ejecuta el command.
7. **El atleta pertenece al coach:** verificado contra `AthleteProfile` del atleta, que debe tener al coach como coach activo.
8. **No se pueden crear dos planes activos simultáneos para el mismo atleta** en el mismo período (se puede archivar el anterior).
9. **Progresión de carga ≤ 10% semanal** (invariante fisiológica; aplica cuando el plan tiene sesiones con carga calculada). Si se inicializa con sesiones vacías/placeholder, esta invariante aplica al momento de poblarlas, no a la creación.
10. **Cada `PlannedSession` tiene tipo válido** (running, cycling, swimming, strength, rest, cross-training) y duración > 0 (salvo rest/off).

Las invariantes 1-8 se verifican al crear. Las 9-10 se verifican a medida que se agregan sesiones (puede ser en el mismo command o en commands subsecuentes).

---

## Escenarios de aceptación

### E1: Creación exitosa de plan con estructura mínima

**Given** un coach autenticado con ID `coach-uuid-A`
**And** un atleta `atleta-uuid-X` cuyo coach activo es `coach-uuid-A`
**And** el atleta tiene un objetivo activo `objetivo-uuid-1` (medio maratón en 12 semanas)
**When** el coach ejecuta `CreateTrainingPlanCommand` con:
  - `athleteId = atleta-uuid-X`
  - `goalId = objetivo-uuid-1`
  - `startDate = 2026-05-04` (lunes)
  - `endDate = 2026-07-26` (domingo, 12 semanas después)
  - `title = "Medio maratón — primavera 2026"`
  - Sin sesiones iniciales (se llenarán después)
**Then** se crea un `TrainingPlan` con ID nuevo
**And** el plan tiene 12 `TrainingWeek`s vacías cubriendo el rango
**And** el plan queda en estado `Active`
**And** se persiste en la DB
**And** se emite `TrainingPlanCreatedDomainEvent` (dentro de Coaching)
**And** se emite `TrainingPlanCreatedIntegrationEvent` (para Communication e Intelligence)
**And** el command retorna `Result<TrainingPlanId>.Success` con el ID del plan creado

### E2: Rechazo por atleta que no pertenece al coach

**Given** un coach autenticado `coach-uuid-A`
**And** un atleta `atleta-uuid-Z` cuyo coach activo es `coach-uuid-B` (otro coach)
**When** el coach `coach-uuid-A` ejecuta `CreateTrainingPlanCommand` para `atleta-uuid-Z`
**Then** el command retorna `Result.Failure(CoachingErrors.AthleteNotOwnedByCoach)`
**And** no se crea plan
**And** no se emite ningún evento
**And** se registra el intento en audit log (posible intento de acceso cruzado entre tenants)

### E3: Rechazo por fechas inválidas

**Given** un coach autenticado con un atleta válido
**When** el coach intenta crear un plan con `startDate = 2026-05-04` y `endDate = 2026-05-03` (endDate antes de startDate)
**Then** el command retorna `Result.Failure(CoachingErrors.InvalidDateRange)`
**And** no se crea plan

### E4: Rechazo por duración fuera de rango

**Given** un coach autenticado con un atleta válido
**When** el coach intenta crear un plan con `endDate - startDate = 380 días` (más de 52 semanas)
**Then** el command retorna `Result.Failure(CoachingErrors.PlanDurationExceedsMaximum)`
**And** no se crea plan

**Escenario simétrico:** duración menor a 7 días → `Result.Failure(CoachingErrors.PlanDurationBelowMinimum)`.

### E5: Rechazo por superposición con plan activo existente

**Given** un coach `coach-uuid-A` con atleta `atleta-uuid-X`
**And** existe un plan activo `plan-uuid-1` para `atleta-uuid-X` desde `2026-05-01` hasta `2026-07-31`
**When** el coach intenta crear otro plan para el mismo atleta con `startDate = 2026-06-01`
**Then** el command retorna `Result.Failure(CoachingErrors.ActivePlanAlreadyExists)`
**And** no se crea el segundo plan
**And** el mensaje incluye el `TrainingPlanId` del plan activo existente (para facilitar UX de "archivar el anterior")

### E6: Caso borde — fechas ajustadas automáticamente a inicio/fin de semana

**Given** un coach autenticado con atleta válido
**When** el coach crea un plan con `startDate = 2026-05-06` (miércoles) y `endDate = 2026-06-02` (martes)
**Then** el command ajusta automáticamente las fechas a límites de semana
**And** el plan queda con `startDate = 2026-05-04` (lunes anterior) y `endDate = 2026-06-07` (domingo siguiente)
**And** el warning "Fechas ajustadas a límites de semana" se devuelve en el `Result.Success` (no es error, es información)

**Nota:** el comportamiento de ajuste automático es una decisión de UX. Alternativa: rechazar con error y pedir al usuario que corrija. Ver "Preguntas abiertas".

---

## Casos de uso / operaciones involucradas

### Command: CreateTrainingPlanCommand

- **Input:**
  - `CoachId` (implícito, del usuario autenticado).
  - `AthleteId` (Guid): atleta para quien se crea el plan.
  - `GoalId` (Guid): objetivo del atleta al que apunta el plan.
  - `StartDate` (DateOnly): fecha de inicio.
  - `EndDate` (DateOnly): fecha de fin.
  - `Title` (string, 3-100 chars): título del plan.
  - `Description` (string?, max 500 chars): descripción opcional.
  - `AutoAdjustWeekBoundaries` (bool, default true): si ajustar fechas a inicio/fin de semana.
- **Output:** `Result<CreateTrainingPlanResult>` donde `CreateTrainingPlanResult` contiene:
  - `PlanId` (TrainingPlanId).
  - `AdjustedStartDate`, `AdjustedEndDate` (si hubo ajuste).
  - `WeeksCreated` (int).
  - `Warnings` (list<string>): avisos no bloqueantes.
- **Precondiciones:** coach autenticado, atleta existe y pertenece al coach, objetivo existe y es del atleta.
- **Postcondiciones:** plan creado en estado `Active`, semanas vacías creadas, eventos emitidos.
- **Errores esperables:**
  - `CoachingErrors.AthleteNotOwnedByCoach`
  - `CoachingErrors.AthleteNotFound`
  - `CoachingErrors.GoalNotFound` / `GoalNotAssociatedWithAthlete`
  - `CoachingErrors.InvalidDateRange`
  - `CoachingErrors.PlanDurationExceedsMaximum`
  - `CoachingErrors.PlanDurationBelowMinimum`
  - `CoachingErrors.ActivePlanAlreadyExists`

### Query: GetTrainingPlanByIdQuery

**(Relacionada pero spec aparte — no parte de esta feature, solo se menciona por completitud.)**

---

## Eventos emitidos

### Domain events (in-process, dentro de Coaching)

- **`TrainingPlanCreatedDomainEvent`**
  - Emitido: al crearse el plan.
  - Datos: `TrainingPlanId`, `AthleteId`, `CoachId`, `StartDate`, `EndDate`.
  - Consumidores internos: actualmente ninguno. Punto de extensión para lógica futura dentro de Coaching (ej. inicialización de biblioteca específica del coach).

### Integration events (cross-context, via outbox)

- **`TrainingPlanCreatedIntegrationEvent`**
  - Emitido: al completarse la transacción de creación.
  - Datos: `TrainingPlanId`, `AthleteId`, `CoachId`, `TenantId`, `StartDate`, `EndDate`, `Title`, `CreatedAt`.
  - Consumidores:
    - **Communication:** para generar notificación al atleta ("tu coach creó un nuevo plan para vos").
    - **Intelligence:** para inicializar `CoachStyleProfile` si es el primer plan del coach y/o pre-cargar contexto analítico.

---

## Eventos consumidos

Esta feature no consume eventos; es el punto de entrada del flujo.

---

## Autorización y multi-tenancy

- **Quién puede ejecutar:** usuarios con rol `Coach` autenticados.
- **Ámbito:** el coach solo puede crear planes para atletas que le pertenecen (verificado contra `AthleteProfile.CoachId`).
- **Policy:** `CanManageAthleteTrainingPlan` con requirement que valida `CoachId == authenticatedUser.CoachId && athlete.CoachId == authenticatedUser.CoachId`.
- **Tenant isolation:** el plan se crea con el `TenantId` del coach. RLS de PostgreSQL bloquea cualquier intento de leer/escribir fuera del tenant.
- **Audit log:** se registra la creación con `CoachId`, `AthleteId`, `PlanId`, timestamp, IP del request.

---

## Consideraciones no funcionales

- **Performance:** creación sincrónica con SLA de p95 < 400ms. Las 12-52 semanas vacías son inserts rápidos (< 100ms incluso para plan largo).
- **Transaccionalidad:** creación del plan + creación de semanas + escritura en outbox → una sola transacción. Si falla algo, rollback completo.
- **Observabilidad:** log estructurado al crear con `{ event: "TrainingPlanCreated", coachId, athleteId, planId, duration }`. Métrica `training_plans_created_total` incrementada.
- **Idempotencia:** no requerida a nivel command (es acción deliberada del coach). Pero si el cliente retries por timeout, el check de "plan activo existente" actúa como protección.

---

## Out of scope

Esta spec **no** cubre:

- Población del plan con sesiones (esa es spec separada, probablemente `002-populate-plan-with-sessions.md`).
- Generación del plan por IA (`generate-training-plan-draft.md`, contexto Intelligence).
- Edición del plan post-creación (`003-adjust-training-week.md` y similares).
- Archivado o eliminación del plan.
- UI del dashboard para creación.
- Notificación efectiva al atleta (solo se emite el evento; Communication decide cómo notificar).
- Templates reutilizables del coach (feature posterior, usará esta spec como base).

---

## Dependencias

- **Specs previas:** ninguna. Esta es una spec fundacional de Fase 1.
- **ADRs aplicables:**
  - ADR-0001 (Monolito modular) — estructura de módulos.
  - ADR-0002 (SDD sobre DDD) — metodología.
- **Servicios externos:** ninguno. Solo DB y outbox local.
- **Módulos backend afectados:** `Coaching` (principal), `AthleteProfile` (via public API para verificar ownership), `Identity` (para autenticación).

---

## Preguntas abiertas

- [x] ¿El auto-ajuste de fechas a límites de semana es silencioso o pide confirmación? → **Resuelto:** silencioso con warning en el Result. La UI puede decidir mostrar el warning o no.
- [ ] ¿Qué pasa si el objetivo del atleta está a más tiempo del plan (ej. objetivo en 6 meses, plan de 4 semanas)? ¿Se permite crear el plan? → **Hipótesis actual:** sí, es válido tener un plan corto dentro de un horizonte largo. Confirmar con coaches en validación.
- [ ] ¿Se permite crear un plan en el pasado (solamente)? Ej. para registrar un ciclo ya ejecutado. → **Hipótesis actual:** no en MVP, feature de "import de plan histórico" es aparte.

---

## Criterios de Done

- [ ] Tests unitarios del agregado `TrainingPlan` cubren todas las invariantes 1-8.
- [ ] Test unitario del método factory `TrainingPlan.Create(...)` con todos los escenarios.
- [ ] Tests unitarios del handler `CreateTrainingPlanCommandHandler` con mocks.
- [ ] Tests de integración con Testcontainers: crea plan, verifica persistencia + outbox + eventos.
- [ ] Tests de aceptación E1-E6 implementados como integration tests.
- [ ] Endpoint `POST /api/v1/training-plans` expuesto con autenticación y autorización.
- [ ] Request/response DTOs con validación FluentValidation.
- [ ] Migration de DB aplicada (tabla `coaching.training_plans`, `coaching.training_weeks`, `coaching.outbox_messages`).
- [ ] OpenAPI actualizado.
- [ ] Logs estructurados implementados.
- [ ] Métrica `training_plans_created_total` expuesta.
- [ ] Audit log registra creación.
- [ ] Code review aprobado.
- [ ] Estado de esta spec actualizado a `Implemented`.

---

## Notas de implementación

- **Convención de semana lunes-domingo:** es estándar en el dominio deportivo europeo/latinoamericano. Usuarios con calendario domingo-sábado (US-centric) no aplican al mercado objetivo inicial. Si se expande a US, reevaluar.
- **`TrainingPlan.Create(...)` como método factory estático en el agregado**, no constructor público. Permite encapsular la lógica de creación incluyendo generación de semanas vacías.
- **`TrainingWeek` como entity dentro del agregado, no agregado separado.** Su ciclo de vida depende del plan.
- **Validación de "activeplan exists" requiere query al repository antes de crear.** Patrón: en handler, no en el constructor del agregado (la invariante depende de contexto externo).
- **Emisión de integration event via outbox:** dentro del mismo DbContext transaction que persiste el agregado. Ver ADR-0005 (por escribir) sobre outbox pattern.
- **Referencia a `GoalId` del contexto AthleteProfile:** se guarda como Guid, no como FK de DB (cross-schema). Verificación de existencia via `IAthleteProfileApi.GoalExistsAsync(goalId, athleteId)` en el handler.
- **Performance considerada:** creación de 52 semanas vacías = 52 inserts. Usar `AddRange` de EF para batch insert, no loop con SaveChanges.

---

## Historial de cambios

| Fecha | Cambio | Razón |
|-------|--------|-------|
| 2026-04-22 | Creación | Initial draft (Felipe) |
| 2026-04-22 | Resolución pregunta abierta sobre auto-ajuste | Decisión de UX tomada |
| 2026-04-22 | Estado → Accepted | Revisión completada |
