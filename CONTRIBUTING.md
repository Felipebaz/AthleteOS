# Cómo contribuir a AthleteOS

Gracias por considerar contribuir. Este documento describe el proceso y las convenciones. Si sos un agente de IA (Claude Code), leé primero `CLAUDE.md`.

## Antes de empezar

1. Leé `README.md` para entender el proyecto.
2. Leé `docs/ARCHITECTURE.md` (al menos niveles 1-5) para entender el modelo.
3. Revisá `docs/adr/` para ver decisiones ya tomadas.
4. Revisá `specs/` para ver specs de features pendientes o en curso.

## Flujo de trabajo

### Para un bugfix trivial

1. Crear branch `fix/descripcion-corta` desde `develop`.
2. Hacer el fix.
3. Agregar test que previene regresión.
4. Commit con Conventional Commits en español.
5. PR a `develop`.

### Para una feature nueva

1. **Escribir spec** en `specs/<bounded-context>/NNN-nombre.md` usando el template (`specs/_template.md`).
2. Marcar la spec como `Review` y solicitar revisión.
3. Una vez aceptada (estado `Accepted`), escribir plan de implementación.
4. Crear branch `feature/<slug>` desde `develop`.
5. Implementar siguiendo el plan y las reglas de `CLAUDE.md`.
6. Tests cubriendo todos los escenarios de aceptación de la spec.
7. PR a `develop` referenciando la spec.
8. Al mergear, actualizar estado de la spec a `Implemented`.

### Para una decisión arquitectónica

1. Copiar `docs/adr/000-template.md` a `docs/adr/NNNN-titulo.md`.
2. Completar: contexto, alternativas, decisión, consecuencias.
3. PR a `develop` para discusión.
4. Al aprobar, actualizar estado a `Aceptado` y agregar al índice en `docs/adr/README.md`.

## Convenciones de commits

Seguimos **Conventional Commits en español**. Formato:

```
<tipo>(<scope>): <descripción corta en imperativo>

<cuerpo opcional: qué y por qué>

<footer opcional: refs, breaking changes>
```

### Tipos válidos

| Tipo | Cuándo |
|------|--------|
| `feat` | Feature nueva |
| `fix` | Bugfix |
| `refactor` | Cambio de código sin cambio de comportamiento |
| `test` | Agregar o corregir tests |
| `docs` | Cambios en documentación |
| `chore` | Tareas de mantenimiento (deps, config) |
| `perf` | Mejora de performance |
| `build` | Cambios en build system |
| `ci` | Cambios en CI/CD |
| `style` | Formateo, sin cambio de lógica |

### Scope

Corresponde al módulo o área afectada: `coaching`, `intelligence`, `training-data`, `identity`, `api`, `web-coach`, `pwa-athlete`, `infra`, `ci`, `adr`, `spec`, `deps`, etc.

### Ejemplos

```
feat(coaching): agregar AdjustTrainingWeekCommand con validación de invariantes

Implementa RF-PLAN-04 según spec en specs/coaching/002-adjust-training-week.md.
Respeta invariantes de progresión de carga y recuperación entre intensidades.
Emite TrainingPlanAdjustedIntegrationEvent.

Closes #42
```

```
fix(training-data): corregir deduplicación de actividades entre Strava y Garmin
```

```
docs(adr): agregar ADR-0005 sobre outbox pattern
```

```
chore(deps): actualizar MediatR a 12.2.0
```

## Branches

Seguimos Gitflow adaptado:

- `main` — producción. Solo acepta merges de `release/*` o `hotfix/*`.
- `develop` — integración. Acepta merges de `feature/*` y `fix/*`.
- `feature/<slug>` — features nuevas.
- `fix/<slug>` — bugfixes no urgentes.
- `hotfix/<slug>` — parches urgentes desde `main`.
- `release/<version>` — estabilización pre-producción.

**Nunca commitear directo a `main` ni a `develop`.**

## Code review

Todo PR necesita al menos una aprobación. Criterios:

- ¿Resuelve lo que dice resolver?
- ¿Los tests cubren los casos importantes?
- ¿Respeta la arquitectura (ver `docs/ARCHITECTURE.md` nivel 4)?
- ¿Sigue las convenciones (este documento + `.editorconfig`)?
- ¿No introduce deuda técnica sin justificación?
- ¿Documentación actualizada si aplica?

## Tests

- **Unit tests:** obligatorios en Domain y Application. Cobertura objetivo 80%+.
- **Integration tests:** para repositorios, handlers de eventos, integraciones externas.
- **E2E tests:** para flujos críticos del usuario.
- **Regla:** si se agrega código sin tests, hay que justificar por qué en el PR.

## Setup del entorno de dev

Ver `README.md` sección "Setup local".

## Preguntas

Si algo no está claro:

1. Revisá la documentación (`docs/`, `CLAUDE.md`, `specs/`).
2. Si no encontrás respuesta, abrí un issue con la etiqueta `question`.

## Código de conducta

Este proyecto sigue un código de conducta básico: comunicación respetuosa, crítica constructiva, sin ataques personales. Discusiones técnicas con argumentos, no con opiniones. Disagreements se resuelven con datos o experimentos, no con autoridad.

---

*Este documento evoluciona. Si encontrás fricción con algún proceso, proponé cambios via PR.*
