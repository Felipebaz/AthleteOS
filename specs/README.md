# Specs

Este directorio contiene las **specs** (especificaciones) de cada feature del sistema. Son el artefacto primario del flujo de desarrollo.

Si no leíste antes, arrancá por:
- **ADR-0002** (`docs/adr/0002-sdd-sobre-ddd.md`) — decisión metodológica de combinar SDD con DDD.
- **`docs/ARCHITECTURE.md`** — el modelo de dominio que estas specs respetan.
- **`CLAUDE.md`** — reglas operativas para el agente.

---

## ¿Qué es una spec?

Una spec describe una feature en términos de **comportamiento del dominio**, no de implementación técnica. Responde:

- ¿Qué hace esta feature en lenguaje del negocio?
- ¿A qué bounded context pertenece?
- ¿Qué agregados toca?
- ¿Qué invariantes preserva?
- ¿Qué escenarios la verifican?
- ¿Qué eventos emite?

Una spec NO describe:

- Archivos, clases, métodos concretos (eso va en el plan de implementación).
- Decisiones arquitectónicas amplias (eso va en ADRs).
- UI / wireframes (eso va en design docs aparte).

---

## ¿Por qué existen las specs?

Tres razones concretas:

1. **Contexto para el agente de IA.** Claude Code genera código coherente cuando tiene contexto explícito. La spec es ese contexto, estructurado y revisable.
2. **Fuente de verdad sobre comportamiento.** Cuando algo no funciona como se espera, se revisa la spec. Si la spec decía eso, el código está mal. Si la spec no lo decía, la spec estaba incompleta.
3. **Tests derivados automáticamente.** Los escenarios Given/When/Then de la spec se traducen directo a tests de aceptación. Las invariantes declaradas se vuelven tests unitarios del agregado.

---

## Cuándo escribir una spec

### Escribir spec

- Features nuevas que afectan al dominio.
- Nuevos casos de uso (commands o queries).
- Nuevos eventos (domain o integration).
- Cambios significativos a un agregado existente.
- Cualquier feature que se vaya a delegar al agente de IA para implementación.

### No escribir spec

- Bugfixes triviales (typo, off-by-one, parseo mal).
- Refactors puros sin cambio de comportamiento (esos pueden requerir ADR si son arquitectónicos).
- Cambios de configuración, dependencias, infraestructura.
- Ajustes de UI/UX que no afectan el backend.
- Mejoras de performance que no cambian el contrato.

**Regla práctica:** si vas a pedirle al agente "implementá X" y X tiene lógica de dominio, escribí la spec antes.

---

## Cómo escribir una spec

### Paso 1: Determinar el bounded context

Antes de escribir nada, preguntarse: **¿a qué bounded context pertenece esta feature?**. Si toca varios, decidir cuál es el "dueño" principal y qué partes se resuelven por eventos hacia los otros.

Si no podés responder con claridad, el modelo estratégico tiene un hueco. Actualizá `ARCHITECTURE.md` antes de seguir.

### Paso 2: Copiar el template

```
cp specs/_template.md specs/<contexto>/<numero>-<nombre-kebab>.md
```

Numeración: secuencial dentro del bounded context, con padding de ceros (`001`, `002`, ..., `025`).

### Paso 3: Completar secciones en orden

Seguir el template de arriba hacia abajo. El orden importa porque cada sección informa a la siguiente:

1. **Metadata** (estado inicial: `Draft`).
2. **Contexto de dominio** — bounded context, agregado, capacidades de negocio.
3. **Historia de usuario** — en términos de dominio, no de UI.
4. **Invariantes preservadas** — cuáles del agregado, si hay nuevas marcarlas.
5. **Escenarios de aceptación** — mínimo 3: camino feliz, error, caso borde.
6. **Casos de uso involucrados** — commands y queries.
7. **Eventos emitidos y consumidos**.
8. **Autorización y multi-tenancy**.
9. **Consideraciones no funcionales** solo si aplican.
10. **Out of scope** — explícito.
11. **Dependencias**.
12. **Preguntas abiertas** si las hay.
13. **Criterios de Done**.
14. **Notas de implementación** (opcional).

### Paso 4: Revisar la spec antes de implementar

Checklist de revisión:

- [ ] ¿Respeta las fronteras del bounded context?
- [ ] ¿Usa lenguaje ubicuo (nombres del negocio, no genéricos)?
- [ ] ¿Las invariantes están claras y son verificables?
- [ ] ¿Los escenarios son completos (feliz + errores + bordes)?
- [ ] ¿Los escenarios son testeables (Given/When/Then bien definidos)?
- [ ] ¿Está claro qué eventos emite y quién los consume?
- [ ] ¿El out-of-scope evita scope creep?
- [ ] ¿Las preguntas abiertas tienen owner?

Si algún check falla, iterá antes de pasar a implementación.

### Paso 5: Cambiar estado a `Accepted` y delegar al agente

Una vez revisada, la spec pasa a `Accepted`. Ahora se puede:

1. Escribir el plan de implementación (estructura técnica).
2. Delegar al agente de IA con: spec + plan + `CLAUDE.md` + `ARCHITECTURE.md`.
3. Revisar el código generado contra la spec.

### Paso 6: Actualizar estado a `Implemented` cuando el PR se mergee

Y si durante implementación se descubrió algo que refina el modelo, actualizar `ARCHITECTURE.md` o agregar ADR **en el mismo PR o uno siguiente**.

---

## Organización del directorio

```
specs/
├── README.md                                        ← este archivo
├── _template.md                                     ← template para copiar
├── identity/
│   ├── 001-coach-registration.md
│   └── 002-athlete-invitation.md
├── athlete-profile/
│   ├── 001-create-athlete-profile.md
│   └── 002-update-training-zones.md
├── training-data/
│   ├── 001-connect-strava.md
│   ├── 002-ingest-activity.md
│   └── 003-disconnect-provider.md
├── coaching/
│   ├── 001-create-training-plan.md
│   ├── 002-adjust-training-week.md
│   └── 003-record-session-feedback.md
├── intelligence/
│   ├── 001-calculate-readiness.md
│   └── 002-generate-weekly-suggestions.md
└── communication/
    └── 001-send-plan-adjustment-notification.md
```

Nombres de archivo: `NNN-descripcion-kebab-case.md`.

Un subdirectorio por bounded context, alineado con los módulos del backend.

---

## Estados de una spec

| Estado | Significado |
|--------|-------------|
| `Draft` | En redacción. No implementar todavía. |
| `Review` | Completa pero esperando revisión (mía o de otro dev). |
| `Accepted` | Revisada y lista para implementar. |
| `Implemented` | Feature mergeada a main. |
| `Deprecated` | Feature ya no aplica; se conserva por historia. |

---

## Relación con otros artefactos

| Artefacto | Qué captura | Velocidad de cambio |
|-----------|-------------|---------------------|
| `docs/ARCHITECTURE.md` | Modelo estratégico, estructura, principios | Baja (meses) |
| `docs/adr/*.md` | Decisiones arquitectónicas individuales | Baja, una por decisión |
| `specs/**/*.md` | Comportamiento de features concretas | Alta (por feature) |
| Código en `src/` | Implementación que respeta todo lo anterior | Muy alta |

**Flujo de influencia:** ARCHITECTURE → ADRs → specs → código.

**Flujo de aprendizaje (feedback):** código → descubrimiento → spec refinada → a veces ARCHITECTURE actualizado o nuevo ADR.

---

## Sobre cómo trabajar con Claude Code usando specs

El flujo típico es:

```
1. Escribir spec en specs/<contexto>/NNN-nombre.md, estado Draft.
2. Autorevisión. Pasar a Review.
3. Si hay otro reviewer, esperar. Pasar a Accepted cuando esté aprobada.
4. Escribir plan de implementación (puede ser en el mismo PR que la spec).
5. Abrir sesión con Claude Code:
   - Referenciar: @specs/<contexto>/NNN-nombre.md
   - Referenciar: @CLAUDE.md
   - Instrucción: "Implementá la spec siguiendo el plan. Empezá por los tests del agregado. Mostrá diffs antes de aplicar cambios grandes."
6. Revisar código generado:
   - ¿Cumple los escenarios?
   - ¿Respeta invariantes?
   - ¿Sigue las reglas de CLAUDE.md?
7. Tests pasan, code review interno → merge.
8. Actualizar estado de spec a Implemented.
```

**Antipatrón a evitar:** abrir Claude Code y pedirle "implementá la feature de ajustar plan" sin spec. El agente va a inventar estructura, y en cada sesión va a inventar una distinta.

---

## Sobre granularidad de specs

Una spec ≠ una user story pequeña, pero tampoco una épica enorme.

**Demasiado grande:** "Sistema de planificación de entrenamiento". Esto es un módulo, no una spec. Se descompone en múltiples specs.

**Demasiado chico:** "Validar que el título del plan no esté vacío". Eso es una invariante, parte de una spec más grande.

**Tamaño correcto:** "Ajustar semana de entrenamiento". Una feature cohesiva, con un command o dos, 3-5 escenarios de aceptación, una historia de usuario clara.

Regla: si la spec entra en ~2-4 horas de implementación total, probablemente está en el tamaño correcto.

---

## Mantenimiento del directorio

- Specs implementadas **no se borran**. La historia importa.
- Si una feature cambia de forma breaking, se crea spec nueva (ej. `002-adjust-training-week-v2.md`) y se marca la anterior como `Deprecated` con link a la nueva.
- Cada trimestre, revisar specs `Draft` que nunca avanzaron y decidir: retomar, promover, o descartar.

---

## Proceso para proponer cambios al template

Si durante el uso detectás que al template le falta algo o sobra algo:

1. Abrir issue describiendo la fricción.
2. Discutir en PR con propuesta de cambio a `_template.md`.
3. Una vez aceptado, aplicar retroactivamente a specs futuras (no reescribir las existentes salvo necesidad).

---

*El template es un punto de partida, no una jaula. Si una spec específica necesita secciones adicionales o diferentes, es válido siempre que los fundamentos (contexto de dominio, invariantes, escenarios) estén.*
