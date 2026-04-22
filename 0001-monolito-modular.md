# ADR-001: Monolito modular sobre microservicios

- **Fecha:** 2026-04-22
- **Estado:** Aceptado
- **Decididores:** Felipe
- **Contexto técnico relacionado:** `docs/ARCHITECTURE.md` niveles 5 (Bounded Contexts), 6 (Vista de despliegue), 10 (Módulos del backend).

## Contexto y problema

El sistema modela un dominio con múltiples bounded contexts claramente diferenciados (Identity, Athlete Profile, Training Data, Coaching, Intelligence, Communication, eventualmente Billing). La existencia de fronteras naturales hace tentador implementar cada contexto como un microservicio independiente desde el inicio, siguiendo patrones populares de arquitecturas modernas.

Sin embargo, existen restricciones reales que hacen esta decisión no trivial:

1. **Equipo de una sola persona** en la fase inicial (el founder). Operar múltiples servicios, coordinar deploys, gestionar fallos distribuidos y debuggear problemas cross-servicio supera ampliamente la capacidad operativa de un solo dev.
2. **Fase de validación de producto:** el dominio va a evolucionar significativamente en los primeros 6-12 meses. Las fronteras entre contextos que hoy parecen claras podrían cambiar a medida que entendamos mejor el problema real. Microservicios cristalizan fronteras prematuramente.
3. **Presupuesto acotado:** microservicios multiplican costos de infraestructura (múltiples instancias, networking, observabilidad distribuida, mensajería inter-servicio).
4. **Complejidad operativa:** transacciones distribuidas, sagas, consistencia eventual entre servicios, tracing distribuido, deploys coordinados, versionado de APIs internas.
5. **Requisitos de consistencia:** varias operaciones críticas (matcheo actividad-sesión, cálculo de carga, aplicación de sugerencia) se benefician fuertemente de transacciones locales dentro de un único proceso.

Al mismo tiempo, necesitamos que la arquitectura **no cierre la puerta** a extraer servicios en el futuro si algún contexto necesita escalar independientemente, ser reescrito en otro lenguaje (ej. Intelligence en Python/ML-heavy), o ser operado por un equipo dedicado cuando crezcamos.

## Fuerzas en tensión

- **Simplicidad operativa hoy** vs. **flexibilidad de escala futura**.
- **Velocidad de iteración** (dominio evolutivo) vs. **disciplina de fronteras** (prevenir acoplamiento).
- **Costos operativos bajos** vs. **separación de concerns infraestructural**.
- **Consistencia transaccional fuerte** (dentro de un contexto) vs. **independencia de deploy** (entre contextos).
- **Onboarding de futuros devs con stack familiar** vs. **libertad tecnológica por servicio**.

## Alternativas consideradas

### Alternativa 1: Microservicios desde el día uno

Cada bounded context como servicio independiente, con su propia DB, API, deploy y stack potencialmente distinto. Comunicación via HTTP/gRPC + mensajería asincrónica.

**Pros:**
- Escalamiento independiente por contexto.
- Fallos aislados.
- Libertad tecnológica por servicio.
- Fronteras físicas fuerzan disciplina.

**Contras:**
- Complejidad operativa desproporcionada para 1 dev.
- Costo de infra multiplicado (instancias, load balancers, service mesh, observabilidad distribuida).
- Transacciones distribuidas requieren sagas / orquestación compleja.
- Refactorizar dominio es muchísimo más caro (implica cambiar APIs, deploys coordinados, migraciones en múltiples DBs).
- Debug de flujos cross-servicio es notablemente más difícil.
- Riesgo alto de *distributed monolith*: microservicios acoplados sin los beneficios.

**Por qué no:** el costo operativo y de desarrollo supera ampliamente los beneficios en esta fase. Es una optimización prematura clásica que mató muchos proyectos early-stage.

### Alternativa 2: Monolito tradicional (sin modularización fuerte)

Una solución única con capas técnicas (Controllers, Services, Repositories) pero sin separación explícita por bounded context. Todo convive en el mismo código, todas las clases pueden referenciar todas las demás.

**Pros:**
- Máxima velocidad inicial.
- Simplicidad extrema.
- Cero complejidad operativa.
- Todo en una transacción.

**Contras:**
- Acoplamiento descontrolado a medida que el código crece.
- Fronteras de dominio se erosionan; el lenguaje ubicuo se contamina.
- Imposible extraer un servicio en el futuro sin reescritura profunda.
- Refactors dolorosos porque todo toca todo.
- No aprovecha la claridad del DDD que ya tenemos identificada.

**Por qué no:** desperdicia la ventaja de haber hecho análisis de dominio. A medida que el proyecto crece (y el objetivo es que crezca), el acoplamiento se vuelve ingobernable. Y al no haber fronteras internas, el costo de eventualmente extraer servicios es enorme.

### Alternativa 3: Monolito modular

Una única solución desplegable, pero con separación estricta por bounded context: cada módulo tiene su propio proyecto, su propio schema de DB, sus propios casos de uso, y solo se comunica con otros módulos via (a) integration events asincrónicos o (b) interfaces públicas explícitas (contratos).

**Pros:**
- Simplicidad operativa de monolito.
- Fronteras internas explícitas, verificables por code review y eventualmente por análisis estático.
- Disciplina arquitectónica sin costo distribuido.
- Refactoring del dominio barato mientras todo vive en el mismo proceso.
- Transacciones locales dentro de un módulo (que es donde las necesitamos).
- Camino de evolución claro: extraer módulo a microservicio cuando haya justificación.
- Se alinea perfecto con DDD estratégico.

**Contras:**
- Requiere disciplina para respetar fronteras (no hay separación física que las fuerce).
- Escalar requiere escalar todo el monolito (todos los módulos suben y bajan juntos).
- Un bug en un módulo puede tumbar todo el proceso.
- Una base de datos compartida (aunque con schemas separados) tiene acoplamiento operativo (migraciones, backups, límites de conexión).

### Alternativa 4: Microservicios selectivos (híbrido)

Monolito principal + algunos servicios extraídos desde el inicio (ej. AI worker como servicio Python separado).

**Pros:**
- Flexibilidad tecnológica donde importa (ML/IA en Python).
- Menos servicios que microservicios puros.

**Contras:**
- Complejidad operativa sigue siendo significativa.
- Decidir qué extraer hoy implica suponer qué va a necesitar escalar en el futuro.
- Para un dev solo, aún son dos cosas distintas que operar.

**Por qué no ahora:** defensible como evolución futura (extraer AI worker a Python cuando los modelos propios lo justifiquen), pero no como punto de partida.

## Decisión

**Construir el sistema como monolito modular, con un proyecto .NET por bounded context, schemas de base de datos separados por contexto, y comunicación inter-módulo exclusivamente via integration events asincrónicos o public contracts.**

La estructura de carpetas y la regla de "módulos no se referencian entre sí en código" están documentadas en `docs/ARCHITECTURE.md` nivel 10 y en `CLAUDE.md`.

Los workers asincrónicos (SyncWorker, AnalysisWorker, AIWorker, etc.) son procesos separados pero comparten el mismo código base y las mismas librerías de módulo. Son "facetas" del mismo monolito con distintos puntos de entrada.

Esta decisión se toma reconociendo que los trade-offs son correctos para la fase actual (1 dev, validación de producto, dominio evolutivo), no como una posición ideológica. Si las condiciones cambian significativamente, esta decisión se reevalúa.

## Consecuencias

### Positivas

- **Operabilidad viable para un solo dev.** Un proceso principal + pool de workers es manejable. Un incidente de producción es debuggeable en un terminal.
- **Costos iniciales de infraestructura bajos.** Menos de 100 USD/mes soporta el MVP completo.
- **Dominio refactorable.** Mover código entre módulos es una operación de IDE, no un proyecto de migración.
- **Transacciones locales donde se necesitan.** Aplicar una sugerencia de IA al plan es atómico dentro del módulo Coaching.
- **Fronteras internas explícitas.** La disciplina de "módulos no se referencian directamente" fuerza un diseño limpio y prepara el terreno para extraer servicios cuando haga falta.
- **Onboarding simple.** Un dev nuevo clona un repo, levanta Docker Compose, y tiene todo funcionando en minutos.
- **Historia técnica potente para el portfolio.** "Monolito modular con DDD, outbox pattern, eventos integrados, preparado para microservicios" es una narrativa madura que diferencia el proyecto.

### Negativas / Trade-offs aceptados

- **Disciplina dependiente del dev.** Las fronteras entre módulos no están forzadas por red ni por proceso. Un `using` indebido entre módulos compila. Mitigación: linting custom + code review + tests de arquitectura (usando NetArchTest o similar).
- **Escalamiento uniforme.** Si Intelligence (CPU-intensivo por LLMs) necesita más recursos, toda la aplicación escala con él. Mitigación: los workers están separados del API, así que se puede escalar el AnalysisWorker sin tocar el API.
- **Fallo compartido.** Un bug de memoria en Coaching tumba también Identity. Mitigación: manejo robusto de errores, circuit breakers en llamadas externas, supervisión.
- **Base de datos compartida.** Aunque con schemas separados, una migration mal hecha o una query pesada afecta a otros contextos. Mitigación: limits de conexión por módulo, query timeouts, aislamiento con RLS.
- **Tentación de atajos.** Es fácil "solo esta vez" romper la regla y hacer un query cross-schema o un `using` cross-módulo. Mitigación: reglas claras en CLAUDE.md + tests de arquitectura + review.

### Neutrales

- La estructura de carpetas es más elaborada que un monolito tradicional, pero más simple que microservicios.
- El outbox pattern y el event bus son necesarios incluso en monolito modular (para comunicación inter-módulo robusta). Esto es overhead que no tendría un monolito tradicional, pero es infraestructura que se paga una sola vez.

## Cuándo reevaluar esta decisión

Esta decisión se revisa si se cumple alguna de estas condiciones:

1. **Escala de equipo:** más de 5-8 developers trabajando activamente en el código. La coordinación en un monolito se vuelve fricción.
2. **Escala de carga:** un contexto específico (probablemente Intelligence) requiere tecnología o escalamiento fundamentalmente distinto del resto (ej. necesitamos Python + GPUs para modelos propios).
3. **Fronteras estables:** el dominio se estabiliza y las fronteras no cambian por 6+ meses. Desaparece el argumento de "dominio evolutivo".
4. **Problemas de disponibilidad:** un bug en un módulo tumba frecuentemente el sistema completo, y el costo de esto supera el costo operativo de separar.
5. **Requisitos de compliance:** algún cliente enterprise exige aislamiento físico de sus datos o su procesamiento.
6. **Madurez operativa:** tenemos SRE, observabilidad distribuida avanzada, y experiencia operando sistemas distribuidos.

El primer candidato a extracción, cuando llegue el momento, probablemente sea **Intelligence** (por razones de stack ML y escalamiento de LLMs). El segundo candidato razonable es **TrainingData** (por volumen de ingesta y necesidad de throughput dedicado).

## Referencias

- *Building Microservices* — Sam Newman (capítulos sobre cuándo NO hacer microservicios).
- *Monolith to Microservices* — Sam Newman (el camino de evolución desde monolito).
- *Implementing Domain-Driven Design* — Vaughn Vernon (bounded contexts como fronteras de servicio).
- Modular Monolith: A Primer — Kamil Grzybek (serie de artículos de referencia).
- *.NET Microservices: Architecture for Containerized .NET Applications* — Microsoft (libro gratuito, capítulo sobre monolito modular).
- `docs/ARCHITECTURE.md` nivel 4 (Principios arquitectónicos, en particular P8: "Diseñado para un dev hoy, extensible a equipo de 10 mañana").
