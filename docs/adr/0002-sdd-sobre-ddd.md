# ADR-0002: Spec-Driven Development sobre base Domain-Driven Design

- **Fecha:** 2026-04-22
- **Estado:** Aceptado
- **Decididores:** Felipe
- **Contexto técnico relacionado:** `docs/ARCHITECTURE.md` nivel 4 (Principios arquitectónicos, P1: DDD como norte), `CLAUDE.md` (reglas operativas para agentes de IA), ADR-0001 (Monolito modular).

## Contexto y problema

El proyecto va a construirse con asistencia intensiva de agentes de IA, específicamente Claude Code, como una herramienta central del flujo de desarrollo. Esto plantea una pregunta metodológica que no resuelve ni DDD ni ningún proceso ágil tradicional: **¿cómo delegar trabajo de implementación a un agente de IA preservando calidad arquitectónica y coherencia de dominio?**

Las opciones típicas fallan de formas identificables:

1. **Uso naíf del agente** (prompts conversacionales tipo "implementá la feature de ajustar plan"): produce código funcional pero inconsistente, con tendencia a transaction scripts, violaciones silenciosas de fronteras arquitectónicas, e invención de estructuras que ya existen con otro nombre.
2. **DDD puro sin metodología de desarrollo**: excelente para el modelado estratégico, pero no define cómo convertir el modelo en código de forma repetible. Sin proceso, el agente no tiene mecanismos para respetar el modelo.
3. **Proceso ágil tradicional** (user stories en backlog, implementación directa): pensado para humanos con contexto acumulado, no para agentes que necesitan contexto explícito en cada interacción.

Existe una metodología emergente, **Spec-Driven Development (SDD)**, formalizada en los últimos años (GitHub Spec Kit, Amazon Kiro, iniciativas de Anthropic) diseñada específicamente para desarrollo asistido por IA. El principio central: **la especificación es el artefacto primario, el código es una expresión generada de la spec**.

El problema: SDD aplicado naíf tiende a producir código orientado a features sin modelo de dominio rico. Specs típicas de SDD describen comportamiento externo sin restricciones internas, lo que invita al agente a elegir estructuras expedientes pero incorrectas.

**La pregunta a resolver:** cómo combinar DDD (para estructura y modelado) con SDD (para flujo de desarrollo con IA) de manera que se potencien en lugar de competir.

## Fuerzas en tensión

- **Velocidad de desarrollo con agente de IA** vs. **disciplina arquitectónica y de dominio**.
- **Flexibilidad de prompts conversacionales** vs. **reproducibilidad y auditabilidad del proceso**.
- **Autonomía del agente** vs. **control humano sobre decisiones de modelado**.
- **Specs detalladas al punto de prescribir código** vs. **specs a nivel de comportamiento que dejan espacio al agente**.
- **Proceso formal documentado** vs. **agilidad para un dev solo**.

## Alternativas consideradas

### Alternativa 1: Desarrollo conversacional directo (no-SDD)

Uso del agente como pair programmer conversacional. Cada feature se discute en chat, se itera, se implementa sin spec formal previa.

**Pros:**
- Máxima velocidad percibida.
- Flexibilidad total.
- Cero overhead metodológico.

**Contras:**
- Imposible auditar por qué el código es como es.
- Decisiones se pierden en el historial de chat.
- Agente genera código inconsistente entre sesiones.
- Sin contrato explícito, el agente inventa estructuras.
- Refactorings masivos posteriores al descubrir inconsistencias.
- Alto riesgo de transaction scripts disfrazados de DDD.

**Por qué no:** el costo de inconsistencia y rework supera la velocidad percibida. Además, pierde el valor de auditabilidad que necesita un proyecto con aspiración comercial y portfolio.

### Alternativa 2: DDD puro con implementación manual

Modelado DDD completo, uso del agente solo como asistente de tipeo ocasional. El dev escribe todo el código a mano.

**Pros:**
- Control total sobre cada línea.
- Código altamente consistente con el modelo.
- No hay riesgo de invenciones del agente.

**Contras:**
- Desperdicia la capacidad del agente.
- Velocidad de desarrollo de un solo dev sin asistencia intensiva.
- No escala a las ambiciones del proyecto (MVP en 6 meses con 1 dev).
- Volumen de código boilerplate que puede generarse automáticamente se escribe a mano.

**Por qué no:** en 2026, con agentes capaces, renunciar a asistencia intensiva es autolimitación. Hay que aprender a colaborar con el agente, no evitarlo.

### Alternativa 3: SDD puro sin base de dominio

Adoptar SDD tal como viene (specs orientadas a features, planes de implementación, generación) sin framework DDD detrás.

**Pros:**
- Proceso claro y repetible.
- Alta velocidad con agente.
- Trazabilidad spec → código.

**Contras:**
- Sin bounded contexts, el agente mezcla responsabilidades.
- Sin agregados, no hay invariantes; el código termina siendo CRUD procedural.
- Sin lenguaje ubicuo, los nombres derivan a genéricos (UserService, DataManager).
- Evolución del producto se vuelve dolorosa porque el modelo no refleja el negocio.
- A largo plazo, produce el mismo acoplamiento que un monolito no modular.

**Por qué no:** el dominio de coaching de endurance es complejo (invariantes fisiológicas, periodización, carga de entrenamiento). Un modelo débil produce software que no escala intelectualmente con el problema.

### Alternativa 4: DDD estratégico + SDD táctico (combinación disciplinada)

Mantener DDD como framework de modelado y estructura (bounded contexts, agregados, eventos, lenguaje ubicuo), y usar SDD como metodología de desarrollo dentro de ese framework. Cada spec se escribe en el lenguaje ubicuo de DDD, respeta los bounded contexts, y opera sobre agregados ya identificados.

**Pros:**
- Aprovecha ambas metodologías en los niveles donde brillan.
- Agente tiene contexto explícito (el modelo DDD) y proceso claro (el flujo SDD).
- Specs son auditables y trazables.
- Modelo de dominio se refina controladamente a medida que se aprende.
- Escalable a equipo: otros devs (humanos o agentes) leen specs y producen código consistente.
- Separa claramente las decisiones de modelado (lentas, profundas, hechas por el dev con criterio) de las de implementación (rápidas, procedurales, asistibles por agente).

**Contras:**
- Requiere disciplina para escribir specs antes de pedir código.
- Overhead inicial de escribir la spec antes de implementar.
- Necesita infraestructura: directorio `specs/`, templates, revisión.
- Curva de aprendizaje para el propio dev sobre cómo escribir buenas specs.

### Alternativa 5: BDD (Behavior-Driven Development) como equivalente

Usar BDD con Gherkin (Given/When/Then) como spec de features.

**Pros:**
- Lenguaje de aceptación claro y ejecutable.
- Madurez de herramientas (SpecFlow en .NET, Cucumber).

**Contras:**
- BDD se centra en escenarios de aceptación, no cubre todos los aspectos de una spec (invariantes de dominio, eventos emitidos, casos de uso, out-of-scope).
- Es complementario, no sustituto.
- BDD no define un proceso de colaboración con agentes de IA.

**Por qué no como solución única:** BDD es valioso pero incompleto. Los escenarios Given/When/Then se incorporan *dentro* de la spec de SDD como sección de escenarios de aceptación, pero no reemplazan al spec completo.

## Decisión

**Adoptar Domain-Driven Design como framework estratégico (modelado, estructura, lenguaje) y Spec-Driven Development como metodología táctica (flujo de desarrollo con asistencia de IA).**

Operativamente, esto significa:

1. **El modelado estratégico vive en `docs/ARCHITECTURE.md` y se refina con cuidado.** Bounded contexts, agregados, eventos, invariantes, lenguaje ubicuo: todos modelados deliberadamente. Cambios al modelo requieren reflexión y eventual ADR.

2. **Cada feature se desarrolla siguiendo el flujo SDD adaptado:**
   - Escribir spec de feature en `specs/<bounded-context>/<numero>-<nombre>.md`.
   - La spec usa lenguaje ubicuo de DDD, declara bounded context y agregados involucrados, lista invariantes preservadas, define escenarios Given/When/Then, enumera eventos emitidos.
   - Escribir plan de implementación (estructura técnica derivada de la spec).
   - Delegar implementación al agente con spec + plan + `CLAUDE.md` como contexto.
   - Revisar código generado contra spec.
   - Si se descubre algo que refina el modelo, actualizar `ARCHITECTURE.md` o abrir ADR.

3. **Las specs son artefactos versionados y revisables.** Se commitean como código. Se refinan con PRs. Son la fuente de verdad sobre qué hace cada feature.

4. **El agente de IA opera dentro del framework, no fuera.** No inventa bounded contexts, no redefine agregados, no cambia el lenguaje ubicuo sin consentimiento explícito. `CLAUDE.md` codifica estas restricciones.

5. **Escenarios de aceptación (Given/When/Then) son parte obligatoria de la spec** y se traducen directamente a tests de integración/aceptación.

## Consecuencias

### Positivas

- **Velocidad alta con calidad alta.** El agente es productivo porque tiene contexto rico; el código es consistente porque el framework está claro.
- **Auditabilidad completa.** Cada feature tiene spec + plan + código + tests. Se puede reconstruir el razonamiento meses después.
- **Onboarding claro.** Un dev nuevo (o agente nuevo) lee `ARCHITECTURE.md`, `CLAUDE.md`, y las specs existentes, y sabe cómo contribuir.
- **Refinamiento del dominio controlado.** Descubrimientos durante implementación se capitalizan refinando `ARCHITECTURE.md`, no dejándose en el código.
- **Portfolio diferenciador.** "Construido con SDD+DDD, flujo riguroso de spec → plan → implementación asistida" es una historia de ingeniería madura.
- **Tests derivados de specs.** Los escenarios Given/When/Then se convierten en tests de aceptación automáticamente.
- **Escalabilidad a equipo.** El proceso no depende del dev original; es repetible por cualquiera.

### Negativas / Trade-offs aceptados

- **Overhead por feature pequeña.** Escribir spec para cambios triviales (typo fix, ajuste cosmético) es excesivo. Mitigación: definir qué cambios ameritan spec y cuáles no (guía en el README del directorio `specs/`).
- **Disciplina dependiente del dev.** Nada fuerza que las specs se escriban antes del código. Mitigación: regla cultural explícita, code review rechaza PRs de features nuevas sin spec asociada.
- **Curva de aprendizaje.** Escribir buenas specs es una habilidad. Las primeras van a ser imperfectas. Mitigación: iterar sobre el template, revisar specs existentes antes de escribir nuevas.
- **Riesgo de bikeshedding en specs.** Gastar demasiado tiempo perfeccionando la spec en vez de implementar. Mitigación: timebox de spec (máx 1-2 horas para features normales).
- **Specs pueden quedar desincronizadas del código.** Si se modifica código sin actualizar la spec, la spec pierde valor. Mitigación: definir que la spec es fuente de verdad; cambios importantes se hacen en la spec primero.

### Neutrales

- El directorio `specs/` crece con el tiempo. Se organiza por bounded context para mantener navegabilidad.
- Algunas specs pueden evolucionar en múltiples versiones (v1, v2) si la feature se rehace. Se conserva historial.
- No todas las features tienen spec. Cambios puramente técnicos (refactor, mejora de performance, dependency update) no requieren spec aunque pueden requerir ADR si son arquitectónicos.

## Cuándo reevaluar esta decisión

Esta decisión se revisa si se cumple alguna de estas condiciones:

1. **El overhead de escribir specs supera el valor.** Si pasan semanas donde el dev evita tareas porque "escribir la spec es mucho trabajo", el proceso está mal calibrado.
2. **Las specs no se respetan.** Si el código generado diverge sistemáticamente de lo especificado y nadie lo corrige, el framework no está funcionando.
3. **El agente no puede trabajar con el nivel de detalle de las specs.** Si las specs no son suficientes para que el agente genere código correcto, hay que repensar el template.
4. **Surge una metodología claramente superior.** Si aparece algo que combina mejor DDD y desarrollo asistido por IA, considerar migración.
5. **Equipo crece mucho.** Con 10+ devs, el proceso puede requerir formalización adicional (revisión de specs por otro dev, por ejemplo).

## Referencias

- GitHub Spec Kit — https://github.com/github/spec-kit
- Amazon Kiro (Spec-Driven Development) — documentación pública de AWS.
- Anthropic Skills — patrones de colaboración con Claude en desarrollo.
- *Implementing Domain-Driven Design* — Vaughn Vernon.
- *Domain-Driven Design Distilled* — Vaughn Vernon.
- *Specification by Example* — Gojko Adzic (sobre BDD y specs ejecutables).
- `docs/ARCHITECTURE.md` niveles 4 (principios) y 5 (bounded contexts).
- `CLAUDE.md` sección "Reglas de arquitectura no negociables" y "Cuando te pido implementar algo nuevo".
- ADR-0001 (Monolito modular) — establece el contexto de simplicidad operativa al que SDD+DDD se adapta.
