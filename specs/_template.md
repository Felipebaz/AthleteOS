# SPEC NNN: Título de la feature

> Template de spec siguiendo el flujo SDD sobre base DDD (ver ADR-0002).
> Una spec describe **comportamiento** en lenguaje ubicuo de dominio, no estructura técnica.

---

## Metadata

- **ID:** SPEC-{contexto}-{numero}
- **Estado:** Draft | Review | Accepted | Implemented | Deprecated
- **Autor:** Felipe
- **Fecha creación:** YYYY-MM-DD
- **Última actualización:** YYYY-MM-DD
- **Issues/PRs relacionados:** #NN

---

## Contexto de dominio

- **Bounded context:** (Identity | AthleteProfile | TrainingData | Coaching | Intelligence | Communication | Billing)
- **Agregado(s) afectado(s):** nombre del agregado raíz que se crea o modifica
- **Capacidades de negocio cubiertas:** referencias a `docs/ARCHITECTURE.md` nivel 2 (ej. RF-PLAN-04)
- **Flujo end-to-end relacionado:** si aplica, referencia al nivel 12 del ARCHITECTURE

---

## Historia de usuario

Formato clásico pero en términos de dominio:

> Como **[rol: coach / atleta / admin]**, quiero **[capacidad en lenguaje de negocio]**, para **[beneficio concreto]**.

Ejemplo:
> Como coach, quiero ajustar las sesiones de una semana del plan de un atleta, para responder a cómo se sintió esa semana y adaptar la carga a las próximas.

---

## Invariantes preservadas

Lista las invariantes del/los agregado(s) que esta feature **debe respetar**. Referenciar las invariantes ya documentadas en `ARCHITECTURE.md` nivel 11 y/o declarar invariantes nuevas si esta feature las introduce.

Ejemplo:
- Incremento de carga semanal ≤ 10% salvo flag explícito del coach (regla fisiológica estándar).
- No hay 2 sesiones de alta intensidad consecutivas sin recuperación ≥24h.
- Sesiones ya completadas no se modifican; cualquier cambio crea versión nueva del plan.

Si esta feature introduce invariantes nuevas al dominio, marcarlas claramente con **[NUEVA]** y asegurarse de actualizar `ARCHITECTURE.md` en el mismo PR.

---

## Escenarios de aceptación

Formato Given/When/Then. Estos escenarios se traducen directamente a tests de aceptación. **Cada spec debe tener al menos 3 escenarios:** el camino feliz, un caso de error esperable, y un caso borde.

### E1: [nombre corto del escenario, camino feliz]

**Given** [estado inicial del sistema relevante]
**And** [condición adicional si aplica]
**When** [acción que dispara el caso de uso]
**Then** [resultado esperado]
**And** [resultado secundario si aplica]

### E2: [escenario de error/violación de invariante]

...

### E3: [caso borde o edge case]

...

### E4: [opcional, más escenarios si la feature lo amerita]

---

## Casos de uso / operaciones involucradas

Lista los commands y queries que implementan esta feature. Para cada uno:

### Command: NombreDelCommand

- **Input:**
  - `Campo1` (tipo): descripción.
  - `Campo2` (tipo): descripción.
- **Output:** `Result<TipoDeRetorno>`
- **Precondiciones:** qué tiene que ser verdad antes de ejecutarse.
- **Postcondiciones:** qué es verdad después de ejecución exitosa.
- **Errores esperables:** qué errores puede devolver (como `Result.Failure`).

### Query: NombreDeQuery

- **Input:** parámetros.
- **Output:** DTO de respuesta.
- **Consideraciones:** autorización, caching, etc.

---

## Eventos emitidos

### Domain events (in-process, dentro del bounded context)

- `NombreDelDomainEvent`: cuándo se emite, qué datos lleva, quién lo consume internamente.

### Integration events (cross-context, via outbox)

- `NombreDelIntegrationEvent`: cuándo se emite, qué datos lleva, qué contextos lo consumen.

---

## Eventos consumidos

Si esta feature reacciona a eventos de otros contextos, listarlos aquí:

- `NombreDelEvento` emitido por `OtroContexto`: cómo reacciona esta feature cuando lo recibe.

---

## Autorización y multi-tenancy

- **Quién puede ejecutar:** rol(es) permitido(s).
- **Ámbito:** qué datos puede ver/modificar. Respeto de tenant isolation.
- **Reglas especiales:** menciones de `CanManageAthlete`, `CanViewPlan`, etc.

---

## Consideraciones no funcionales

Solo si son relevantes para esta feature. No copiar todo de `ARCHITECTURE.md`.

- **Performance:** SLA específico si difiere del default.
- **Seguridad:** datos sensibles involucrados, manejo especial.
- **Privacidad:** consentimientos, retención.
- **Observabilidad:** métricas o logs específicos que esta feature debe exponer.

---

## Out of scope

Lista explícita de lo que esta feature **no** hace. Evita scope creep y deja claro el alcance.

Ejemplo:
- Sugerencias de IA para el ajuste (esa es feature aparte del contexto Intelligence).
- Notificación al atleta del cambio (lo maneja Communication escuchando el evento).
- UI del dashboard (feature de frontend aparte).

---

## Dependencias

- **De otras features/specs:** si esta depende de que otras estén implementadas primero.
- **De decisiones arquitectónicas (ADRs):** si aplica algún ADR específico.
- **De servicios externos:** si requiere nueva integración o consume API externa.

---

## Preguntas abiertas

Si hay aspectos de la spec no resueltos todavía, declararlos acá para no olvidar. Antes de marcar la spec como `Accepted`, estas preguntas deben tener respuesta.

- [ ] ¿Qué pasa en el caso X que no está cubierto arriba?
- [ ] ¿Se requiere migration de datos existentes?

---

## Criterios de Done

- [ ] Tests unitarios del/los agregado(s) cubren invariantes declaradas.
- [ ] Tests de aceptación cubren todos los escenarios E1..EN.
- [ ] Tests de integración validan persistencia y eventos.
- [ ] Endpoints de API expuestos si aplica, con autorización correcta.
- [ ] Observabilidad instrumentada (logs estructurados, métricas si aplica).
- [ ] Documentación de API actualizada (OpenAPI).
- [ ] `ARCHITECTURE.md` actualizado si hubo refinamiento del modelo.
- [ ] ADR creado si hubo decisión arquitectónica nueva.
- [ ] Code review aprobado.

---

## Notas de implementación

Espacio libre para consideraciones que el implementador (humano o agente) debe tener en cuenta, sin ser parte del contrato de la spec.

Ejemplo:
- "Considerar usar specification pattern para la validación de progresión de carga porque se reutiliza en otras features."
- "La librería X tiene un bug conocido en el método Y, evitar usarla."

---

## Historial de cambios

| Fecha | Cambio | Razón |
|-------|--------|-------|
| YYYY-MM-DD | Creación | Initial draft |
