# Architecture Vision Document

> **Proyecto:** Plataforma SaaS de coaching inteligente para deportes de endurance
> **Nombre de trabajo (placeholder):** `AthleteOS` / `CoachLens` — a definir tras validación
> **Autor:** Felipe
> **Versión:** 0.1 — Draft inicial
> **Última actualización:** 2026-04
> **Audiencia:** el founder (yo mismo), futuros colaboradores técnicos, agentes de IA asistiendo el desarrollo (Claude Code)

---

## Cómo leer este documento

Este documento va de lo más abstracto a lo más concreto, en 15 niveles. Cada nivel responde una pregunta distinta:

1. **Visión del producto** — ¿Qué problema resolvemos y para quién?
2. **Requisitos funcionales** — ¿Qué hace el sistema?
3. **Requisitos no funcionales** — ¿Cómo se comporta?
4. **Principios arquitectónicos** — ¿Qué reglas guían cada decisión?
5. **Bounded contexts** — ¿Cuáles son las partes del negocio?
6. **Vista de despliegue lógica** — ¿Cómo se empaqueta y corre?
7. **Vista de datos** — ¿Qué tipos de datos manejamos?
8. **Vista de seguridad** — ¿Cómo protegemos todo?
9. **Riesgos técnicos** — ¿Qué puede salir mal?
10. **Módulos del backend** — ¿Cómo se estructura el código del servidor?
11. **Modelo de dominio por contexto** — ¿Qué agregados, eventos y casos de uso hay?
12. **Flujos end-to-end críticos** — ¿Qué pasa cuando un atleta sube una actividad?
13. **Clean Architecture interna y tecnologías** — ¿Qué stack, qué patrones?
14. **Infraestructura, CI/CD y ambientes** — ¿Cómo se despliega y opera?
15. **Roadmap y fases de construcción** — ¿En qué orden se construye esto?

Los primeros 9 niveles son **invariantes del producto**: no dependen del lenguaje ni del stack. Los niveles 10 en adelante bajan al detalle técnico.

---

# Nivel 1 — Visión del producto

## 1.1 Problema

Los coaches de deportes de endurance (running, ciclismo, triatlón, natación de aguas abiertas) que entrenan atletas a distancia enfrentan tres problemas sistémicos:

1. **Sobrecarga de análisis manual.** Un coach con 15-25 atletas activos dedica entre 6 y 17 horas semanales solo a mirar datos de Garmin/Strava, comparar con lo planificado, detectar desviaciones y ajustar planes.

2. **Detección tardía de señales críticas.** Sobreentrenamiento, lesiones inminentes, enfermedad subclínica y estancamiento de rendimiento aparecen primero en los datos (HRV, calidad de sueño, variabilidad de FC, adherencia). El coach las ve con días o semanas de retraso porque revisa los datos de forma inconsistente.

3. **Herramientas fragmentadas y obsoletas.** TrainingPeaks es el estándar pero tiene UX de 2012 y cero inteligencia predictiva. Muchos coaches (especialmente hispanohablantes) terminan usando Excel + WhatsApp + Strava, con toda la fricción que eso implica.

## 1.2 Propuesta de valor

Un sistema que:

1. **Ingiere automáticamente** los datos de entrenamiento y salud de todos los atletas de un coach desde sus wearables (Garmin, Strava, Polar, Suunto, COROS).
2. **Los analiza continuamente** calculando carga, readiness, tendencias y detectando anomalías.
3. **Presenta al coach un dashboard priorizado** con "estos 3 atletas necesitan tu atención esta semana y por qué", en vez de obligarlo a revisar 25 atletas uno por uno.
4. **Sugiere ajustes concretos al plan** que el coach aprueba, modifica o rechaza con un click, nunca se aplican sin revisión humana.
5. **Aprende del coach** con cada aceptación/rechazo, adaptándose a su estilo y filosofía.

La propuesta en una frase: **el coach recupera horas semanales y aumenta su capacidad de atender más atletas sin bajar la calidad del coaching.**

## 1.3 Stakeholders

| Stakeholder | Rol | Quién decide la compra | Valor recibido |
|-------------|-----|------------------------|----------------|
| **Coach independiente** | Usuario primario pagante | Sí | Tiempo recuperado, escalabilidad de su negocio, mejor retención de atletas |
| **Atleta del coach** | Usuario consumidor | No (pero influye) | Plan más adaptativo, feedback más rápido, mejor experiencia |
| **Club / Organización** | Comprador institucional (fase 2) | Sí | Monitoreo de plantel, prevención de lesiones a nivel equipo |
| **Administrador** | Operador de la plataforma | N/A | Herramientas de soporte y monitoreo |

## 1.4 Métricas norte del producto

Sin estas métricas clavadas, toda decisión de arquitectura es opinión:

| Métrica | Objetivo MVP (6 meses) | Objetivo año 1 |
|---------|------------------------|-----------------|
| Horas/semana ahorradas por coach | 3+ | 6+ |
| Tasa de aceptación de sugerencias de IA | 50%+ | 70%+ |
| Coaches activos pagantes | 10-20 | 100+ |
| Atletas por coach promedio | 10-15 | 18-25 |
| Retención mensual de coaches | 85%+ | 92%+ |
| NPS de coaches | 30+ | 50+ |
| Incidentes de sobreentrenamiento/lesión detectados vs. baseline manual | +30% | +60% |

## 1.5 Lo que NO es el producto (explícitamente)

Es tan importante definir esto como definir lo que sí es, para resistir scope creep:

- **No es una app para el atleta final** (no competimos con Strava ni con Garmin Connect).
- **No es una plataforma de clases o reservas** (no competimos con Trainerize ni MindBody).
- **No es una red social deportiva** (no competimos con Strava como feed).
- **No es un producto para coaches de gimnasio / fuerza pura** (en MVP; eso es expansión futura).
- **No genera planes sin supervisión humana.** El coach siempre aprueba.
- **No da consejos médicos ni diagnostica.** Detecta patrones, sugiere, deriva.

---

# Nivel 2 — Requisitos funcionales

Los requisitos se agrupan por **capacidades de negocio**, no por features de UI. Una capacidad es algo que el sistema permite, independiente de cómo se implemente.

## 2.1 Capacidad: Gestión de identidades y accesos

- RF-IAM-01: Coach se registra con email + password (u OAuth Google/Apple).
- RF-IAM-02: Coach invita atletas por email con link único y expiración.
- RF-IAM-03: Atleta acepta invitación creando cuenta o iniciando sesión.
- RF-IAM-04: Coach puede gestionar roles dentro de su organización (futuro).
- RF-IAM-05: Cada coach es un tenant aislado; un coach nunca ve atletas de otro.
- RF-IAM-06: Admin del sistema puede impersonar usuarios para soporte (con auditoría).
- RF-IAM-07: Recuperación de password con token de uso único.
- RF-IAM-08: MFA opcional para coaches, obligatorio para admins.
- RF-IAM-09: Atleta puede terminar su relación con un coach y migrar a otro coach conservando historial.

## 2.2 Capacidad: Perfil deportivo del atleta

- RF-ATH-01: Registrar datos biométricos básicos (edad, peso, género, altura).
- RF-ATH-02: Registrar y actualizar zonas de entrenamiento (FC, ritmo, potencia) manualmente o por test.
- RF-ATH-03: Registrar historial de lesiones con fechas y estado actual.
- RF-ATH-04: Definir uno o más objetivos activos (evento, fecha, meta).
- RF-ATH-05: Configurar disponibilidad semanal (días/horas entrenables).
- RF-ATH-06: Mantener baselines actualizados (HRV, FC reposo, sueño promedio).
- RF-ATH-07: Registrar equipamiento relevante (zapatillas km, bicicleta km — fase 2).

## 2.3 Capacidad: Ingesta de datos externos

- RF-ING-01: Atleta conecta Strava via OAuth 2.0.
- RF-ING-02: Atleta conecta Garmin Connect via OAuth.
- RF-ING-03: Ingesta automática de actividades nuevas sin intervención manual.
- RF-ING-04: Ingesta de métricas diarias de salud (sueño, HRV, stress, pasos).
- RF-ING-05: Normalización de datos heterogéneos a un modelo canónico interno.
- RF-ING-06: Detección y descarte de duplicados entre proveedores.
- RF-ING-07: Upload manual de archivos .fit/.gpx/.tcx como fallback.
- RF-ING-08: Reprocesamiento histórico a demanda (última semana, último mes, todo).
- RF-ING-09: Detección y notificación de desconexiones (token expirado, revocado).

## 2.4 Capacidad: Planificación de entrenamiento

- RF-PLAN-01: Coach crea plan para un atleta con rango de fechas y objetivo.
- RF-PLAN-02: Plan se estructura en semanas; cada semana contiene sesiones diarias.
- RF-PLAN-03: Cada sesión tiene tipo, duración objetivo, intensidad objetivo, descripción.
- RF-PLAN-04: Coach edita plan; cada edición crea versión nueva (historial inmutable).
- RF-PLAN-05: Coach guarda bloques reutilizables ("biblioteca" de sesiones/semanas/mesociclos).
- RF-PLAN-06: IA genera borrador de plan a partir de objetivo + historial; coach aprueba/edita.
- RF-PLAN-07: Plan respeta reglas fisiológicas duras (progresión de carga ≤10% semanal sin flag explícito).
- RF-PLAN-08: Atleta ve solo su plan activo; no accede a versiones antiguas por defecto.

## 2.5 Capacidad: Seguimiento de ejecución

- RF-EXEC-01: Matcheo automático de actividad ingestada con sesión planificada por fecha + tipo.
- RF-EXEC-02: Matcheo manual cuando el automático falla (coach o atleta).
- RF-EXEC-03: Cálculo de desviación (volumen, intensidad, cumplimiento) planificado vs. ejecutado.
- RF-EXEC-04: Atleta reporta wellness subjetivo diario (RPE, sueño, ánimo, dolor muscular).
- RF-EXEC-05: Atleta agrega notas de texto a sesiones ejecutadas.
- RF-EXEC-06: Cálculo diario de métricas derivadas (TSS, CTL, ATL, TSB o equivalentes).
- RF-EXEC-07: Marcado de sesiones: completada, parcial, saltada, reemplazada.

## 2.6 Capacidad: Análisis inteligente

- RF-AI-01: Cálculo nocturno de "readiness score" por atleta (0-100).
- RF-AI-02: Detección de anomalías en HRV, FC reposo, sueño, carga.
- RF-AI-03: Identificación de estancamiento (sin mejora en métrica clave durante N semanas).
- RF-AI-04: Generación de sugerencias priorizadas para el coach, con razonamiento textual + datos.
- RF-AI-05: Categorías de sugerencia: ajuste de sesión, ajuste de bloque, derivación profesional, sin acción.
- RF-AI-06: Registro de feedback del coach (aceptada, modificada, rechazada + razón opcional).
- RF-AI-07: Aprendizaje incremental del estilo del coach a partir de su feedback.
- RF-AI-08: Confidence score en cada sugerencia (baja confianza → se muestra pero con disclaimer).
- RF-AI-09: Explicabilidad obligatoria: toda sugerencia muestra qué datos la soportan.

## 2.7 Capacidad: Comunicación coach-atleta

- RF-COM-01: Mensajería asincrónica en hilo por atleta.
- RF-COM-02: Mensajes pueden referenciar sesiones o métricas específicas.
- RF-COM-03: Notificaciones push (web push + email como fallback).
- RF-COM-04: Preferencias de notificación por canal y tipo.
- RF-COM-05: Coach responde preguntas con contexto (IA sugiere respuesta; coach aprueba/edita).

## 2.8 Capacidad: Facturación y suscripciones (fase 2)

- RF-BILL-01: Planes segmentados por cantidad de atletas gestionados.
- RF-BILL-02: Suscripciones mensuales y anuales con descuento anual.
- RF-BILL-03: Pagos recurrentes via Stripe (+ MercadoPago para LatAm).
- RF-BILL-04: Self-service de upgrade, downgrade, cancelación.
- RF-BILL-05: Generación de factura fiscal (Uruguay, Argentina, España como países prioritarios).
- RF-BILL-06: Grace period de 7 días ante falla de cobro.
- RF-BILL-07: Reactivación de cuenta tras pago recuperado conserva todo el historial.

## 2.9 Capacidad: Reportes y exportaciones

- RF-REP-01: Reporte semanal automático para atleta (resumen + insights).
- RF-REP-02: Reporte mensual para coach (KPIs de su operación).
- RF-REP-03: Exportación completa de datos del atleta (portabilidad legal).
- RF-REP-04: Exportación del plan en formato estándar (PDF + JSON).

## 2.10 Capacidad: Administración de plataforma

- RF-ADM-01: Panel de admin interno con métricas de plataforma.
- RF-ADM-02: Logs de auditoría accesibles para admins.
- RF-ADM-03: Gestión de feature flags.
- RF-ADM-04: Herramientas de soporte (ver estado de un tenant, resetear sincronizaciones).

---

# Nivel 3 — Requisitos no funcionales

Esta es la sección que separa proyectos juniors de proyectos serios. Los NFR deben ser **medibles y verificables**, no vaguedades.

## 3.1 Disponibilidad

| Aspecto | Meta MVP | Meta producto maduro |
|---------|----------|----------------------|
| Uptime mensual | 99.0% (7h downtime/mes) | 99.9% (43min/mes) |
| RTO (Recovery Time Objective) | 4 horas | 30 minutos |
| RPO (Recovery Point Objective) | 24 horas | 1 hora |
| Mantenimiento planificado | Anunciado 72h antes | Idem + ventana de baja demanda |

**Ventana crítica:** 05:00-10:00 UTC-3 (mañana hispana) es cuando coaches revisan datos nocturnos; ningún mantenimiento ahí.

## 3.2 Performance

| Operación | p50 | p95 | p99 | Timeout |
|-----------|-----|-----|-----|---------|
| Login | 200ms | 500ms | 1s | 5s |
| Dashboard coach (vista principal) | 500ms | 1.5s | 3s | 10s |
| Vista detalle de atleta | 400ms | 1s | 2s | 8s |
| Guardar ajuste de plan | 150ms | 400ms | 800ms | 5s |
| Sincronización de actividad (background) | N/A | 5min desde registro | 15min | 1h |
| Generación de sugerencia IA | 3s | 10s | 20s | 60s (async con feedback) |
| Cálculo de readiness nocturno | N/A | 30s/atleta | 2min | 10min |

## 3.3 Escalabilidad

| Métrica | MVP (mes 6) | Año 1 | Año 3 aspiracional |
|---------|-------------|-------|---------------------|
| Coaches activos | 20 | 500 | 5.000 |
| Atletas activos | 300 | 10.000 | 100.000 |
| Actividades ingestadas/mes | 10.000 | 300.000 | 3.000.000 |
| Requests/segundo pico | 5 | 100 | 1.000 |
| Datos de time-series (GB) | 5 | 150 | 1.500 |
| Costo mensual de infraestructura | <100 USD | <2.000 USD | <20.000 USD |

**Principio:** la arquitectura no tiene que *soportar* 5000 coaches hoy, tiene que *poder llegar ahí* sin reescritura profunda.

## 3.4 Seguridad

- Cifrado en tránsito: TLS 1.3 mínimo; HSTS habilitado.
- Cifrado en reposo: AES-256 en DB y object storage.
- Cifrado a nivel aplicación para campos extra-sensibles (tokens OAuth, datos médicos).
- Secretos en vault gestionado (nunca en .env commiteados, nunca en código).
- Rotación automática de secretos críticos cada 90 días.
- MFA opcional coaches, obligatorio admins.
- Rate limiting: 100 req/min por usuario autenticado; 10 req/min por IP no autenticada.
- Protección OWASP Top 10 verificada (inyección, XSS, CSRF, SSRF, etc.).
- Dependency scanning en CI (Snyk, Dependabot, o equivalente).
- Secret scanning en CI (gitleaks o similar).
- Pentesting antes de GA comercial.

## 3.5 Privacidad y cumplimiento

- **Marcos regulatorios aplicables:**
  - Ley 18.331 (Uruguay) — Protección de datos personales.
  - GDPR — si atendemos usuarios europeos (aplicable extraterritorialmente).
  - LGPD (Brasil) — si expandimos allá.
  - Ley 19.628 (Chile), Habeas Data (Argentina).
- **Datos de salud** se tratan como categoría especial (GDPR Art. 9, LGPD Art. 11). Consentimiento explícito requerido.
- Consentimientos versionados y auditables.
- Política de retención explícita:
  - Actividades y datos del atleta: mientras el usuario exista + 30 días post-borrado.
  - Logs de sistema: 30-90 días.
  - Logs de auditoría: 5 años.
  - Backups: rotación de 90 días.
- Derecho al olvido: borrado efectivo (hard delete después de ventana de seguridad).
- Portabilidad: export completo en JSON + CSV en menos de 30 días.
- DPA firmado con todos los proveedores (Anthropic, hosting, email, etc.).

## 3.6 Observabilidad

- Logs estructurados (JSON) centralizados, retención 30 días mínimo.
- Trazabilidad distribuida con correlation ID por request.
- Métricas de negocio (no solo técnicas): sugerencias/día, tasa aceptación, actividades sincronizadas, MAU de coaches.
- Métricas técnicas estándar: latencia, throughput, error rate, saturación (RED + USE).
- Alertas proactivas para error rate > 1%, latencia p95 degradada, colas creciendo sin drenarse.
- Dashboards separados por audiencia: técnico (on-call) y producto (founder).
- Error tracking con contexto (Sentry o equivalente).

## 3.7 Mantenibilidad

- Cobertura de tests: 90%+ en Domain, 70%+ en Application, 60%+ global.
- Setup de dev local en <30 min para un dev nuevo.
- Documentación arquitectónica viva (este documento + ADRs).
- Linter + formatter obligatorios en CI.
- Deploy sin downtime (rolling / blue-green).
- Rollback automatizado en <5 min ante degradación.
- Migraciones de DB siempre reversibles (excepción documentada).

## 3.8 Usabilidad

- Accesibilidad WCAG 2.1 AA como objetivo mínimo.
- Responsive en dashboard del coach (tablet usable, mobile en modo lectura).
- PWA del atleta: mobile-first, instalable, offline para plan de hoy.
- Internacionalización preparada (i18n); lanzamiento solo en español.
- Tiempo de primera interacción útil en móvil 4G: <3 segundos.
- Dark mode soportado.

## 3.9 Portabilidad y vendor lock-in

- Dominio de negocio no depende de ningún cloud específico.
- Core de dominio no depende de LLM específico (abstracción `IInsightGenerator`).
- Migración entre clouds factible en <1 mes ante incidente grave de proveedor.
- Datos siempre recuperables: backups portables (no formato propietario).

## 3.10 Costos operativos

| Componente | Meta por coach/mes | Justificación |
|------------|---------------------|----------------|
| Infra (compute + DB + storage) | <0.50 USD | Margen sano contra suscripción 20-40 USD |
| LLM (inferencias) | <0.30 USD | Caching + modelos adecuados a la tarea |
| Email + push | <0.05 USD | Volumen bajo |
| **Total infra/coach/mes** | **<1 USD** | Gross margin >90% |

---

# Nivel 4 — Principios arquitectónicos

Las reglas que guían toda decisión técnica. Cuando hay duda, se va a estos principios.

## P1. Domain-Driven Design como norte

El código refleja el negocio. Los nombres del código son los nombres que usa el coach. Los módulos se corresponden con contextos de negocio, no con capas técnicas. Si no lo entiende un coach al ver un diagrama de contextos, algo está mal.

**Consecuencia práctica:** no existen clases `UserManager`, `DataProcessor`, `Helper`. Existen `TrainingPlan`, `Athlete`, `ReadinessSnapshot`, `CoachSuggestion`.

## P2. Clean Architecture estricta

Dependencias apuntan **hacia adentro**, al dominio. El dominio no sabe de infraestructura ni de la web. La infraestructura implementa puertos (interfaces) definidos por el dominio o la aplicación.

**Regla de dependencias:**
```
Api ──► Application ──► Domain
 │            │
 └──► Infrastructure ──► Application
                   └──► Domain
```

Domain no depende de nadie. Application depende solo de Domain. Infrastructure depende de Application y Domain. Api depende de todos (compone).

## P3. Event-driven entre contextos, transaccional dentro

- **Dentro de un bounded context:** operaciones transaccionales con garantías ACID.
- **Entre bounded contexts:** eventos asincrónicos con consistencia eventual.
- **Nunca transacciones distribuidas.**

## P4. Consistency boundaries explícitos

Lo que debe ser consistente inmediatamente vive en el mismo agregado. Lo que puede ser eventualmente consistente vive en contextos separados. Un caso de uso modifica **un único agregado**.

## P5. Asíncrono por defecto para operaciones lentas

Ingesta, análisis, generación de IA, notificaciones: todo por colas. El usuario recibe respuesta inmediata ("procesando..."), no espera.

## P6. Seguridad y privacidad como requisitos, no features

Todo diseño pasa por el filtro: ¿qué datos sensibles toca? ¿cómo se protegen? ¿quién accede? ¿cómo se audita? No se agrega después.

## P7. Observabilidad como requisito

Un módulo no está "terminado" si no expone métricas, logs estructurados y traces. Es parte del Definition of Done.

## P8. Diseñado para un dev hoy, extensible a equipo de 10 mañana

Monolito modular por ahora. Fronteras tan claras que partir a microservicios sea mecánico, no rediseño.

## P9. Vendor-agnostic en el core, pragmático en la periferia

El dominio no depende de Anthropic ni de AWS. Los adaptadores sí pueden serlo. Cambiar de LLM o de proveedor de email no toca el dominio.

## P10. Automatización por encima de disciplina

Lo que puede verificarse con un test, CI check o linter, se automatiza. No dependas de que el dev se acuerde.

## P11. Tests como documentación ejecutable

Los tests de dominio describen las reglas de negocio mejor que cualquier comentario. Se leen como especificación.

## P12. Fail loud, fail early

Mejor un error explícito en deploy que un comportamiento raro en producción. Validaciones estrictas en los bordes, invariantes duras en el dominio.

## P13. Datos son el activo más valioso

Ante duda, conservar datos. Backups, audit trail, soft delete cuando aplique, event sourcing parcial para decisiones críticas.

## P14. Evolvability > scalability prematura

No optimizar para 1M de usuarios cuando tenés 10. Pero sí dejar puertas abiertas para cuando llegue ahí.

## P15. El mejor código es el que no existe

Antes de construir algo, preguntarse si existe una solución gestionada que lo resuelva. Auth, pagos, emails, observabilidad: raramente vale la pena construir desde cero.

---

# Nivel 5 — Bounded contexts

## 5.1 Tipos de contextos (Core / Supporting / Generic)

Siguiendo DDD, clasificamos cada contexto según cuánto valor competitivo genera:

| Contexto | Tipo | Construir o comprar |
|----------|------|---------------------|
| Coaching | **Core** | Construir, es el corazón |
| Intelligence | **Core** | Construir, es la ventaja |
| Athlete Profile | Supporting | Construir simple |
| Training Data Ingestion | Supporting | Construir, con énfasis en el anticorruption layer |
| Communication | Supporting | Construir simple, tercerizar delivery |
| Identity & Access | Generic | Considerar comprar (Clerk, Auth0) o construir standard |
| Billing | Generic | Comprar (Stripe) |
| Notification Delivery | Generic | Comprar (Expo Push, SendGrid) |

## 5.2 Descripción de cada contexto

### 5.2.1 Identity & Access Context

**Responsabilidad:** autenticación, autorización, gestión de usuarios, tenancy, invitaciones.

**Lenguaje ubicuo:** Usuario, Coach, Atleta, Tenant, Rol, Permiso, Sesión, Invitación, Token de refresh.

**Límites:**
- **Sí hace:** signup, login, logout, gestión de tokens, gestión de roles, invitaciones, recuperación de password, MFA.
- **No hace:** perfil deportivo (eso es Athlete Profile), preferencias de notificación (Communication), relación coach-atleta como concepto de negocio (Coaching).

**Upstream de:** todos los demás contextos.

---

### 5.2.2 Athlete Profile Context

**Responsabilidad:** perfil deportivo del atleta. Distinto del usuario. Un atleta puede existir como perfil antes de que tenga cuenta activa.

**Lenguaje ubicuo:** Atleta, Zona de entrenamiento, Objetivo, Lesión, Disponibilidad, Test fisiológico, Baseline.

**Límites:**
- **Sí hace:** CRUD de perfil deportivo, gestión de zonas, registro de lesiones, cálculo de baselines a partir de datos históricos.
- **No hace:** autenticar al atleta (Identity), almacenar actividades (Training Data), planificar entrenamiento (Coaching).

**Colabora con:** Coaching (provee info para planificar), Intelligence (provee contexto para análisis).

---

### 5.2.3 Training Data Context

**Responsabilidad:** ingesta, normalización y almacenamiento de datos de wearables y fuentes externas. Es la frontera con el mundo exterior.

**Lenguaje ubicuo:** Actividad, Stream de datos, Métrica de salud, Proveedor, Sincronización, Token externo, Webhook.

**Límites:**
- **Sí hace:** conectar proveedores externos, ingerir actividades, normalizar a modelo canónico, detectar duplicados, almacenar streams de time-series, ofrecer consultas a otros contextos.
- **No hace:** interpretar los datos (Intelligence), relacionarlos con plan (Coaching), mostrarlos al usuario (API + frontend).

**Patrón arquitectónico clave:** **Anticorruption Layer** con un adaptador por cada proveedor externo. Traduce datos heterogéneos al modelo canónico interno (`CanonicalActivity`).

---

### 5.2.4 Coaching Context

**Responsabilidad:** corazón del producto. Planes de entrenamiento, su evolución, ejecución, y la relación coach-atleta.

**Lenguaje ubicuo:** Plan, Periodización, Mesociclo, Semana de entrenamiento, Sesión planificada, Sesión ejecutada, Desviación, Ajuste, Sugerencia aplicada, Template, Biblioteca del coach.

**Límites:**
- **Sí hace:** crear/editar planes, versionar cambios, matchear ejecución con planificación, calcular cumplimiento, gestionar feedback del coach sobre sugerencias.
- **No hace:** almacenar datos crudos de actividades (Training Data), correr análisis predictivo (Intelligence), comunicar con el atleta (Communication).

**Agregados principales:** `TrainingPlan`, `SessionExecution`, `CoachLibrary`.

---

### 5.2.5 Intelligence Context

**Responsabilidad:** análisis, predicciones, sugerencias, aprendizaje del estilo del coach. Donde vive la IA.

**Lenguaje ubicuo:** Readiness, Tendencia, Anomalía, Predicción, Confianza del modelo, Sugerencia, Razonamiento, Feedback loop del coach.

**Límites:**
- **Sí hace:** calcular readiness diario, detectar anomalías, generar sugerencias contextualizadas, aprender del feedback, ofrecer insights para dashboards.
- **No hace:** modificar planes (solo sugiere, Coaching aplica), tomar decisiones autónomas (siempre coach-in-the-loop), almacenar datos crudos.

**Agregados principales:** `AthleteReadinessSnapshot`, `CoachSuggestion`, `CoachStyleProfile`.

---

### 5.2.6 Communication Context

**Responsabilidad:** mensajería entre coach y atleta, notificaciones cross-channel, preferencias.

**Lenguaje ubicuo:** Hilo, Mensaje, Notificación, Canal (push, email, in-app), Preferencia, Entrega.

**Límites:**
- **Sí hace:** gestionar hilos coach-atleta, orquestar envío a canales, gestionar preferencias, registrar entregas.
- **No hace:** entregar físicamente (Notification Delivery tercerizado), decidir qué eventos generan notificaciones (lo decide cada contexto emisor).

---

### 5.2.7 Billing Context (fase 2)

**Responsabilidad:** suscripciones, pagos, facturación.

**Lenguaje ubicuo:** Suscripción, Plan de precio, Período, Cobro, Factura, Cupón, Método de pago.

**Límites:**
- Motor real es Stripe/MercadoPago. Billing Context es el sistema de registro interno que refleja y expone el estado de facturación al resto del negocio.

## 5.3 Mapa de contextos (Context Map)

```
                    ┌────────────────────┐
                    │  Identity & Access │
                    │       (U/S)        │
                    └─────────┬──────────┘
                              │ upstream
                              ▼
       ┌──────────────────────┼──────────────────────┐
       │                      │                      │
       ▼                      ▼                      ▼
┌─────────────┐       ┌─────────────┐        ┌─────────────┐
│   Athlete   │       │   Training  │        │  Coaching   │
│   Profile   │◄─────►│     Data    │        │             │
│             │  U/S  │             │        │   (Core)    │
└──────┬──────┘       └──────┬──────┘        └──────┬──────┘
       │                     │                      │
       │                     │ Published Language    │
       │                     │ (eventos canónicos)   │
       │                     ▼                      │
       │              ┌─────────────┐               │
       └─────────────►│Intelligence │◄──────────────┘
                      │             │  Partnership
                      │   (Core)    │
                      └──────┬──────┘
                             │
                             ▼
                      ┌─────────────┐
                      │Communication│
                      │   (OHS)     │
                      └─────────────┘

              ┌────────────────────────────┐
              │  Proveedores Externos      │
              │  (Garmin, Strava, Polar)   │
              └─────────────┬──────────────┘
                            │
                            │ ACL (Anticorruption Layer)
                            ▼
                    [Training Data]

Leyenda:
U/S = Upstream/Downstream (Customer-Supplier)
OHS = Open Host Service
ACL = Anticorruption Layer
```

**Patrones aplicados:**
- **Customer-Supplier (U/S)** entre Identity y el resto: Identity dicta, los demás consumen.
- **Published Language** entre Training Data e Intelligence/Coaching: eventos canónicos estables como `ActivityIngested`.
- **Partnership** entre Coaching e Intelligence: evolucionan juntos, colaboración fuerte.
- **Open Host Service** con Communication: cualquiera le puede hablar por API estable.
- **Anticorruption Layer** contra proveedores externos: protección del dominio.
- **Conformist** con Stripe (en fase 2): aceptamos su modelo, no luchamos.

---

# Nivel 6 — Vista de despliegue lógica

## 6.1 Componentes de runtime

```
┌───────────────────────────────────────────────────────────────────┐
│                           EDGE / GATEWAY                           │
│  TLS • WAF • Rate limiting • CDN para assets • Reverse proxy      │
└──────────────────────────────┬────────────────────────────────────┘
                               │
         ┌─────────────────────┼─────────────────────┐
         │                     │                     │
         ▼                     ▼                     ▼
┌──────────────────┐   ┌──────────────────┐  ┌──────────────────┐
│  Dashboard Coach │   │   PWA Atleta     │  │  Admin Panel     │
│  (React + Vite)  │   │  (React + Vite,  │  │   (React)        │
│   desktop-first  │   │    PWA install)  │  │                  │
└──────────────────┘   └──────────────────┘  └──────────────────┘
         │                     │                     │
         └─────────────────────┼─────────────────────┘
                               │ HTTPS + JWT
                               ▼
┌───────────────────────────────────────────────────────────────────┐
│                  API PRINCIPAL (.NET 8 stateless)                  │
│                                                                     │
│  ┌────────────┐ ┌────────────┐ ┌────────────┐ ┌────────────┐     │
│  │  BFF Web   │ │ BFF Mobile │ │ Core API   │ │ Admin API  │     │
│  │  (Coach)   │ │  (Atleta)  │ │            │ │            │     │
│  └────────────┘ └────────────┘ └────────────┘ └────────────┘     │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │             MÓDULOS DEL DOMINIO (bounded contexts)            │  │
│  │                                                               │  │
│  │  Identity • AthleteProfile • TrainingData • Coaching •        │  │
│  │  Intelligence • Communication • Billing(fase 2)               │  │
│  └─────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────┘
                               │
                               ▼
┌───────────────────────────────────────────────────────────────────┐
│                    BUS DE EVENTOS / COLAS                          │
│              (Redis Streams MVP → RabbitMQ a escala)               │
└───────────────────────────────────────────────────────────────────┘
                               │
         ┌─────────────┬───────┼───────┬─────────────┐
         │             │       │       │             │
         ▼             ▼       ▼       ▼             ▼
┌──────────────┐ ┌──────────┐ ┌────────────┐ ┌──────────────┐
│ Sync Workers │ │ Analysis │ │ AI Worker  │ │ Notification │
│ (Garmin,     │ │ Worker   │ │ (LLM calls)│ │ Dispatcher   │
│  Strava...)  │ │          │ │            │ │              │
└──────────────┘ └──────────┘ └────────────┘ └──────────────┘
         │             │             │             │
         └─────────────┴─────────────┴─────────────┘
                               │
                               ▼
┌───────────────────────────────────────────────────────────────────┐
│                         ALMACENAMIENTO                             │
│                                                                     │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌────────────┐  │
│  │ PostgreSQL │  │TimescaleDB │  │   Redis    │  │   Object   │  │
│  │(OLTP core) │  │(time-series│  │  (cache)   │  │  Storage   │  │
│  │            │  │ de actividad│  │            │  │ (FIT, etc) │  │
│  └────────────┘  └────────────┘  └────────────┘  └────────────┘  │
└───────────────────────────────────────────────────────────────────┘

┌───────────────────────────────────────────────────────────────────┐
│                     SERVICIOS EXTERNOS                             │
│                                                                     │
│  Anthropic (Claude) • Garmin Connect API • Strava API • Polar     │
│  SendGrid/Resend (email) • Sentry (errors) • OTel backend (obs)   │
│  Stripe (fase 2) • Expo Push (si app nativa, fase 2)              │
└───────────────────────────────────────────────────────────────────┘
```

## 6.2 Topologías por fase

| Fase | Compute | Data | Observación |
|------|---------|------|-------------|
| **MVP** | 1 API + 2 workers, PaaS (Railway/Fly) | PostgreSQL gestionado + Redis | Single region |
| **Tracción inicial (año 1)** | API auto-escalada + pool workers | DB con réplica lectura + Redis cluster | Single region, CDN global |
| **Escala (año 3+)** | Multi-servicio / microservicios extraídos | DB sharded por tenant + read replicas | Multi-region si latencia lo exige |

## 6.3 Principios de deploy

- **Stateless API:** cualquier instancia puede servir cualquier request. Todo estado va a DB o cache.
- **Workers idempotentes:** un mismo mensaje puede procesarse 2 veces sin consecuencias.
- **12-factor app:** config por env vars, logs a stdout, dependencias explícitas.
- **Inmutable deployments:** una imagen Docker por versión, promovida entre ambientes.

---

# Nivel 7 — Vista de datos

## 7.1 Categorías de datos

| Categoría | Volumen | Patrón de acceso | Tecnología |
|-----------|---------|------------------|------------|
| Transaccional de negocio | Medio | Lectura/escritura balanceada | PostgreSQL |
| Series temporales de actividad | Alto | Escritura batch, lectura agregada | TimescaleDB (ext de PostgreSQL) |
| Datos derivados / analíticos | Medio | Lectura pesada, escritura nocturna | PostgreSQL + materialized views + cache Redis |
| Embeddings / vectorial | Bajo-medio | Búsqueda semántica | pgvector (ext PostgreSQL) |
| Archivos binarios | Alto (bytes) | Escritura rara, lectura rara | Object storage (S3, R2) |
| Logs y auditoría | Alto | Append-only, búsqueda ocasional | PostgreSQL partitioned + archivado |

## 7.2 Decisión de multi-tenancy

**Modelo elegido:** **Shared database, shared schema con `tenant_id` en cada tabla.**

**Razones:**
- Simplicidad operativa (una DB, un set de migraciones).
- Costo bajo (no una DB por tenant).
- Facilita analytics cross-tenant (con cuidado).

**Contras aceptados:**
- Riesgo de leak de datos entre tenants si se olvida un filtro (mitigado con RLS o filtros automáticos en repo).
- Ruido en queries si un tenant tiene mucho volumen.

**Implementación:**
- Cada tabla de negocio tiene columna `tenant_id` NOT NULL.
- Row Level Security (RLS) de PostgreSQL habilitado por defecto.
- Session variable `app.current_tenant` seteada en cada request.
- Repositorios en código filtran automáticamente por tenant actual.

**Cuándo reevaluar:** si un cliente enterprise exige aislamiento físico → schema-per-tenant o DB-per-tenant.

## 7.3 Políticas de datos

| Tipo | Retención | Backup | Archivado |
|------|-----------|--------|-----------|
| Actividades del atleta | Vida del usuario + 30d post-borrado | Diario, 90d retenidos | Fríos a >2 años |
| Streams detallados | Idem | Idem | Compresión + fríos a >1 año |
| Mensajes coach-atleta | Vida del usuario + 30d | Diario | Sin archivado |
| Logs de sistema | 30-90 días | No backup (ephemeral) | N/A |
| Logs de auditoría | 5 años | Mensual | Glacial >1 año |
| Backups | Rotación 90 días | Off-site semanal | N/A |

## 7.4 Principios de modelado de datos

1. **UUID v7 como identificadores** (no auto-incrementales ni v4 puro): tiempo-ordenables, sin revelación de volumen, insertables concurrentemente.
2. **Strongly-typed IDs en código**, nunca `Guid` genéricos: `AthleteId`, `TrainingPlanId`.
3. **Timestamps en UTC siempre**. Conversión a zona horaria solo en la UI.
4. **Soft delete con flag `deleted_at`** donde aplica legal o de negocio; hard delete donde se requiere (GDPR/DPA).
5. **Audit fields obligatorios:** `created_at`, `updated_at`, `created_by`, `updated_by`.
6. **Outbox table en cada schema de contexto** para eventos salientes.
7. **Migrations siempre reversibles** salvo excepciones documentadas.

---

# Nivel 8 — Vista de seguridad

## 8.1 Modelo de amenazas (STRIDE resumido)

| Amenaza | Vector principal | Mitigación |
|---------|------------------|------------|
| **S**poofing (suplantación) | Robo de tokens, phishing | JWT corto + refresh rotativo, MFA, HSTS |
| **T**ampering (manipulación) | MITM, inyección | TLS 1.3, prepared statements, validación estricta |
| **R**epudiation (negación) | Operación sin trazabilidad | Audit log de acciones sensibles con user+timestamp+IP |
| **I**nformation disclosure | SQL injection, leak cross-tenant | ORM parametrizado, RLS, principio menor privilegio |
| **D**enial of service | Flood de requests, queries pesadas | Rate limiting, timeouts, circuit breakers, cola bounded |
| **E**levation of privilege | Bug en autorización | Policy-based authz, tests de authz en CI |

## 8.2 Defensa en capas

### Capa 1 — Perímetro
- TLS 1.3 everywhere, HSTS, HPKP opcional.
- WAF gestionado (CloudFlare/Vercel/proveedor).
- Rate limiting por IP y por user.
- Protección DDoS del proveedor cloud.

### Capa 2 — Autenticación
- Password hashing con Argon2id (preferido) o bcrypt cost 12+.
- JWT de acceso con TTL corto (15 min).
- Refresh token rotativo con detección de reuso.
- MFA opcional via TOTP o passkeys.
- Bloqueo tras N intentos fallidos con backoff.

### Capa 3 — Autorización
- Policy-based: cada endpoint declara requirements (`CanManageAthlete`, `CanViewPlan`).
- Multi-tenant enforcement por RLS en DB + filtro en código (double check).
- Principio de menor privilegio en roles de DB.

### Capa 4 — Protección de datos
- Cifrado en reposo (AES-256) en DB y object storage.
- Cifrado a nivel aplicación para tokens OAuth y campos médicos sensibles.
- Vault para secretos (AWS Secrets Manager, Doppler, o similar).
- Rotación de secretos automática cada 90d para secretos de integración.

### Capa 5 — Auditoría
- Log de accesos a datos sensibles (lectura de info de atleta por coach).
- Log de operaciones admin.
- Tamper-evident: append-only, hash chain opcional.
- Retención 5 años.

### Capa 6 — Respuesta a incidentes
- Runbook documentado.
- Responsables definidos (on-call rotation cuando haya equipo).
- Plan de comunicación a usuarios afectados (72h para notificación bajo GDPR).
- Post-mortems sin culpa.

## 8.3 OAuth con proveedores externos (específico)

- Tokens de Garmin/Strava **cifrados a nivel aplicación** antes de persistir.
- Scopes mínimos necesarios.
- Detección de revocación y re-pedido de autorización.
- Nunca logs con tokens.
- Renovación automática de refresh tokens.

---

# Nivel 9 — Riesgos técnicos

## 9.1 Matriz de riesgos

| # | Riesgo | Probabilidad | Impacto | Exposición | Mitigación |
|---|--------|--------------|---------|------------|------------|
| R1 | Garmin/Strava cambia API y rompe ingesta | Media | Alto | **Alta** | ACL, múltiples proveedores, upload manual como fallback |
| R2 | Costos de LLM escalan desproporcionadamente | Alta | Medio | **Alta** | Caching, structured outputs, modelo por tarea, monitoreo costos/tenant |
| R3 | LLM alucina una sugerencia peligrosa | Media | Alto | **Alta** | Coach-in-the-loop siempre, validación estructurada, reglas duras del dominio |
| R4 | Modelado fisiológico incorrecto (TSS, CTL, etc.) | Media | Alto | **Alta** | Consultar 2-3 coaches expertos, validar con literatura, tests con datos reales |
| R5 | Leak de datos de salud | Baja | Catastrófico | **Alta** | Security by design, pentesting, cifrado doble, menor dato posible |
| R6 | Vendor lock-in con LLM específico | Media | Medio | Media | Abstracción `IInsightGenerator`, evaluación periódica de alternativas |
| R7 | Complejidad supera la capacidad del equipo (1 dev) | Alta | Alto | **Alta** | Servicios gestionados, foco en core, recortar features sin piedad |
| R8 | Sobreingeniería prematura | Alta | Medio | **Alta** | Monolito modular, no microservicios hasta dolor real |
| R9 | Baja adopción por coaches (producto no validado) | Media | Catastrófico | **Alta** | Validación con 10+ coaches antes de código, beta paga temprana |
| R10 | Churn por falta de valor sostenido | Media | Alto | Alta | Métricas de engagement desde día 1, check-ins con clientes |
| R11 | Problemas legales por datos de salud | Baja | Catastrófico | Media | Asesor legal local, DPAs, términos claros, consentimientos granulares |
| R12 | Fallo catastrófico de DB sin backup usable | Baja | Catastrófico | Media | Backups diarios probados mensualmente (test de restore real) |

**Regla:** los riesgos marcados con exposición **Alta** deben tener un item concreto en el backlog cada sprint.

## 9.2 Riesgos no técnicos a tener presente

- **Focus drift:** agregar features para un segundo segmento (nutricionistas, gym) antes de dominar el primero.
- **Burnout del founder:** sin ingresos 6+ meses construyendo solo es duro; tener deadlines claros y descansos.
- **Falta de feedback real:** construir en vacío sin coaches reales probando.

---

# Nivel 10 — Módulos del backend

Ahora aterrizamos los bounded contexts a estructura real de código .NET.

## 10.1 Organización de la solución

**Patrón:** monolito modular con un proyecto por capa por bounded context, más proyectos compartidos.

```
src/
├── BuildingBlocks/                           # código compartido, sin dominio
│   ├── BuildingBlocks.Domain/                # base classes: Entity, AggregateRoot, ValueObject, DomainEvent
│   ├── BuildingBlocks.Application/           # MediatR behaviors, abstractions genéricas
│   ├── BuildingBlocks.Infrastructure/        # EF interceptors, outbox base, event bus abstraction
│   └── BuildingBlocks.Api/                   # filtros, middlewares, problem details
│
├── Modules/
│   ├── Identity/
│   │   ├── Identity.Domain/
│   │   ├── Identity.Application/
│   │   ├── Identity.Infrastructure/
│   │   └── Identity.Api/                     # controllers + endpoints
│   │
│   ├── AthleteProfile/
│   │   ├── AthleteProfile.Domain/
│   │   ├── AthleteProfile.Application/
│   │   ├── AthleteProfile.Infrastructure/
│   │   └── AthleteProfile.Api/
│   │
│   ├── TrainingData/
│   │   ├── TrainingData.Domain/
│   │   ├── TrainingData.Application/
│   │   ├── TrainingData.Infrastructure/
│   │   │   └── Integrations/                 # un adaptador por proveedor
│   │   │       ├── Strava/
│   │   │       ├── Garmin/
│   │   │       └── Polar/
│   │   └── TrainingData.Api/
│   │
│   ├── Coaching/
│   │   ├── Coaching.Domain/
│   │   ├── Coaching.Application/
│   │   ├── Coaching.Infrastructure/
│   │   └── Coaching.Api/
│   │
│   ├── Intelligence/
│   │   ├── Intelligence.Domain/
│   │   ├── Intelligence.Application/
│   │   ├── Intelligence.Infrastructure/
│   │   │   └── Llm/                          # abstracciones + impl Anthropic
│   │   └── Intelligence.Api/
│   │
│   └── Communication/
│       ├── Communication.Domain/
│       ├── Communication.Application/
│       ├── Communication.Infrastructure/
│       └── Communication.Api/
│
├── Bootstrap/
│   └── ApiHost/                              # punto de entrada, compone todos los módulos
│
└── Workers/
    ├── SyncWorker/                           # procesa colas de sincronización
    ├── AnalysisWorker/                       # readiness, anomalías
    ├── AiWorker/                             # llamadas a LLM
    ├── NotificationWorker/                   # dispatch de notificaciones
    └── OutboxPublisher/                      # publica eventos de outbox al bus

tests/
├── Modules/
│   ├── Identity.UnitTests/
│   ├── Identity.IntegrationTests/
│   ├── Coaching.UnitTests/
│   └── ...
└── E2E/
    └── CriticalFlows/
```

## 10.2 Reglas de comunicación entre módulos

1. **Módulos no se referencian entre sí en código.** El módulo Coaching no hace `using Intelligence.Domain`. Esto previene dependencias ocultas.

2. **Comunicación entre módulos solo por dos vías:**
   - **Integration Events** (asincrónico, desacoplado, vía bus).
   - **Public API** del módulo, expuesta como interface en un proyecto `ModuleX.Contracts` consumible por otros módulos si hace falta lectura síncrona.

3. **Cada módulo dueño de su schema de DB.** Los schemas se llaman igual que el módulo: `identity`, `coaching`, `intelligence`, etc.

4. **No queries cross-schema.** Si Coaching necesita info de Athlete Profile, la pide via API pública o la recibe via evento.

## 10.3 Composición en el host

El proyecto `ApiHost` es el único que referencia a todos los `*.Api` de los módulos y los compone:

```
Program.cs (pseudocódigo conceptual)
    ↓
builder.Services
    .AddBuildingBlocks()
    .AddIdentityModule(configuration)
    .AddAthleteProfileModule(configuration)
    .AddTrainingDataModule(configuration)
    .AddCoachingModule(configuration)
    .AddIntelligenceModule(configuration)
    .AddCommunicationModule(configuration);
    ↓
app.MapIdentityEndpoints();
app.MapAthleteProfileEndpoints();
// ... etc
```

Cada módulo expone un método de extensión `AddXxxModule` y `MapXxxEndpoints` en su proyecto `.Api`. Esto hace que agregar un módulo nuevo sea una sola línea.

---

# Nivel 11 — Modelo de dominio por contexto

Acá describo los agregados principales, eventos y casos de uso de los contextos core. Los contextos genéricos/supporting se resumen.

## 11.1 Coaching Context — detalle

### Agregados

#### `TrainingPlan` (Aggregate Root)

**Invariantes:**
- Siempre tiene al menos un objetivo asociado.
- Las semanas no se superponen ni dejan huecos en el período del plan.
- Incremento de carga semanal ≤ 10% salvo flag explícito del coach.
- No hay 2 sesiones de alta intensidad consecutivas sin recuperación ≥24h.
- Una sesión completada no se modifica; se crea versión nueva del plan.

**Entidades internas:**
- `TrainingWeek`
- `PlannedSession`

**Value objects:**
- `Intensity` (baja, media, umbral, VO2max, neuromuscular)
- `SessionType` (rodaje, intervalos, tempo, largo, fuerza, descanso, cross-training)
- `Duration`
- `LoadProgression`

**Operaciones clave:**
- `CreateFromTemplate(templateId, athleteId, startDate)`
- `CreateFromAiDraft(aiDraftId, coachAdjustments)`
- `AdjustWeek(weekNumber, changes, reason)`
- `ApplyCoachSuggestion(suggestionId, overrides?)`
- `Archive()`

**Eventos emitidos:**
- `TrainingPlanCreated`
- `TrainingPlanAdjusted`
- `TrainingPlanArchived`
- `PlannedSessionUpdated`

#### `SessionExecution` (Aggregate Root)

**Responsabilidad:** cómo se ejecutó una sesión, matcheada con lo planificado.

**Invariantes:**
- Tiene referencia a `PlannedSessionId` y a una o más `ActivityId` (de Training Data).
- El status deriva automáticamente del match (`Completada`, `Parcial`, `Sustituida`, `Saltada`).
- El feedback subjetivo del atleta solo lo puede escribir el atleta.

**Value objects:**
- `ComplianceScore` (0-100, derivado de volumen e intensidad)
- `SubjectiveFeedback` (RPE, ánimo, sueño, dolor muscular)

**Operaciones clave:**
- `LinkActivity(activityId)`
- `RecordSubjectiveFeedback(feedback)`
- `MarkAsSkipped(reason)`
- `RecalculateCompliance()`

**Eventos emitidos:**
- `SessionExecutionUpdated`
- `SessionDeviationDetected` (cuando compliance < umbral)

#### `CoachLibrary` (Aggregate Root)

Biblioteca del coach con templates de sesión, semanas y mesociclos reutilizables. Agregado simple pero útil de modelar explícitamente.

### Eventos consumidos (de otros contextos)

- `ActivityIngested` → intenta matchear con `PlannedSession`, crea/actualiza `SessionExecution`.
- `HealthMetricsUpdated` → trigger de recomputación de métricas derivadas.
- `CoachSuggestionGenerated` → guardar referencia para que el coach pueda aplicar.

### Casos de uso principales (Application layer)

- `CreateTrainingPlanCommand`
- `AdjustTrainingWeekCommand`
- `ApplyCoachSuggestionCommand`
- `RecordSessionFeedbackCommand` (atleta)
- `GetAthleteWeekViewQuery`
- `GetCoachDashboardQuery`

---

## 11.2 Intelligence Context — detalle

### Agregados

#### `AthleteReadinessSnapshot` (Aggregate Root)

Estado diario de preparación del atleta. Se computa cada noche (o bajo demanda).

**Invariantes:**
- Único por atleta por día.
- Score global (0-100) derivado de componentes.
- Tiene un `confidenceScore` basado en cuántos datos reales hay.

**Componentes calculados:**
- `HrvTrend` (% vs baseline 28 días).
- `SleepQuality` (horas + score del wearable).
- `TrainingLoadBalance` (TSB / CTL ratio).
- `SubjectiveWellness` (del feedback del atleta).

**Flags posibles:** `PossibleOvertraining`, `PossibleIllness`, `HighFatigue`, `LowAdherence`, `NormalState`.

**Eventos emitidos:**
- `ReadinessSnapshotCalculated`
- `AnomalyDetected` (si algún flag crítico)

#### `CoachSuggestion` (Aggregate Root)

Una sugerencia generada por IA para el coach sobre un atleta.

**Invariantes:**
- Tiene razonamiento textual + datos estructurados que lo soportan.
- Tiene categoría (`SessionAdjust`, `BlockAdjust`, `Referral`, `Informational`).
- Tiene `confidenceScore`.
- Estado: `Pending`, `Accepted`, `ModifiedAndAccepted`, `Rejected`, `Expired`.
- El feedback del coach es inmutable una vez dado.

**Operaciones clave:**
- `Accept(coachId)`
- `AcceptWithModifications(coachId, modifications, reason?)`
- `Reject(coachId, reason?)`
- `Expire()` (pasado cierto tiempo sin respuesta)

**Eventos emitidos:**
- `CoachSuggestionGenerated`
- `CoachSuggestionAccepted`
- `CoachSuggestionRejected`

#### `CoachStyleProfile` (Aggregate Root)

Modelo del estilo del coach, construido a partir de sus aceptaciones/rechazos. Usado para personalizar futuras sugerencias.

**Contenido:**
- Pesos aprendidos por tipo de sugerencia.
- Preferencias explícitas (zonas de entrenamiento preferidas, estilos de periodización).
- Estadísticas (tasa de aceptación histórica, tiempo promedio de respuesta).

### Casos de uso principales

- `CalculateReadinessCommand` (disparado por scheduler nocturno)
- `GenerateWeeklyReviewSuggestionsCommand` (sugerencias de ajuste de plan)
- `RecordSuggestionFeedbackCommand`
- `GetPendingSuggestionsQuery`
- `GetAthleteReadinessQuery`

---

## 11.3 Training Data Context — detalle

### Agregados

#### `AthleteDataSource` (Aggregate Root)

La conexión de un atleta con un proveedor externo.

**Contiene:** tokens OAuth (cifrados), scopes, estado, última sincronización, errores recientes.

**Operaciones:** `Connect`, `Disconnect`, `RefreshToken`, `TriggerSync`, `MarkAsFailed`.

#### `Activity` (Aggregate Root)

Una actividad deportiva ingresada al sistema, ya normalizada.

**Invariantes:**
- Único por `(source, externalId)`.
- `startTimestamp < endTimestamp`.
- TSS se calcula internamente, no se recibe del proveedor.

**Value objects:** `ActivityType`, `MetricsSummary`, `GeoRoute`.

**Operaciones:** `LinkToPlannedSession`, `RecalculateMetrics`, `MarkAsManual`.

#### `HealthMetricSnapshot` (Aggregate Root)

Métricas diarias de salud (sueño, HRV, stress, pasos, peso) de un atleta.

### Integración con proveedores externos (ACL)

Patrón por proveedor:

```
Infrastructure/Integrations/Strava/
├── StravaOAuthClient.cs
├── StravaActivityFetcher.cs
├── StravaWebhookReceiver.cs
├── StravaActivityMapper.cs     # traduce de modelo Strava a CanonicalActivity
└── StravaAdapter.cs             # implementa IWearableProvider
```

Cada adaptador implementa la interfaz `IWearableProvider`:

```
IWearableProvider
├── ConnectAsync(authCode, athleteId)
├── FetchActivitiesAsync(since, until)
├── FetchHealthMetricsAsync(date)
├── HandleWebhookAsync(payload)
└── DisconnectAsync()
```

---

## 11.4 Athlete Profile Context — resumen

### Agregado principal

#### `Athlete` (Aggregate Root)

**Contiene:**
- Datos biométricos básicos.
- `TrainingZones` (FC, ritmo, potencia).
- Historial de lesiones (`InjuryRecord[]`).
- Objetivos activos (`Goal[]`).
- Disponibilidad semanal (`WeeklyAvailability`).
- Baselines (`HrvBaseline`, `RestingHrBaseline`).

**Invariantes:**
- Las zonas nunca se superponen incorrectamente.
- No hay 2 objetivos activos con el mismo evento en la misma fecha.
- Los baselines se actualizan solo con datos validados.

**Operaciones:** `RecalibrateZones`, `RegisterInjury`, `UpdateBaseline`, `SetAvailability`.

---

## 11.5 Identity Context — resumen

### Agregados

- `User` — identidad básica y credenciales.
- `Coach` — extensión de User con datos de coach (tenant owner).
- `AthleteAccount` — extensión de User para atletas (puede estar en múltiples coaches a lo largo del tiempo).
- `Invitation` — token de invitación con expiración.
- `Session` / `RefreshToken` — gestión de sesiones.

Si se terceriza a Clerk/Auth0, este contexto se simplifica a un adaptador + el concepto de negocio de "Coach" y "Atleta" extendidos sobre el usuario externo.

---

## 11.6 Communication Context — resumen

### Agregados

- `ConversationThread` — hilo coach-atleta.
- `Message` — mensaje dentro de un thread.
- `Notification` — notificación generada por eventos de otros contextos, con preferencias aplicadas.

### Eventos consumidos de otros contextos

Communication se suscribe a muchos eventos y decide qué notificar:
- `TrainingPlanAdjusted` → notifica al atleta.
- `CoachSuggestionGenerated` (si alta confianza) → notifica al coach.
- `SessionDeviationDetected` → notifica al coach.
- `ActivityIngested` (grandes) → notifica al coach.

---

## 11.7 Integration events (el "lenguaje publicado" entre contextos)

Lista de eventos cross-context estables. Estos son contratos que no cambian a la ligera.

| Evento | Emisor | Consumidores típicos |
|--------|--------|----------------------|
| `UserRegistered` | Identity | AthleteProfile, Communication |
| `AthleteInvitedToCoach` | Identity | AthleteProfile, Coaching |
| `AthleteProfileUpdated` | AthleteProfile | Coaching, Intelligence |
| `DataSourceConnected` | TrainingData | Communication |
| `ActivityIngested` | TrainingData | Coaching, Intelligence |
| `HealthMetricsUpdated` | TrainingData | Intelligence |
| `TrainingPlanCreated` | Coaching | Communication, Intelligence |
| `TrainingPlanAdjusted` | Coaching | Communication, Intelligence |
| `SessionExecutionUpdated` | Coaching | Intelligence |
| `ReadinessSnapshotCalculated` | Intelligence | Coaching (para dashboard) |
| `CoachSuggestionGenerated` | Intelligence | Coaching, Communication |
| `CoachSuggestionAccepted` | Coaching | Intelligence (learning loop) |
| `CoachSuggestionRejected` | Coaching | Intelligence (learning loop) |

**Versionado:** cada evento tiene versión en el schema. Cambios breaking → versión nueva, compatibilidad con versión anterior durante período de transición.

---

# Nivel 12 — Flujos end-to-end críticos

Esta sección es oro para entender el sistema. Escribimos los flujos como secuencias de pasos, nombrando los componentes que participan.

## 12.1 Flujo: Atleta conecta Strava y su primera actividad aparece en dashboard del coach

**Participantes:** PWA Atleta, Identity API, TrainingData API, Strava API, SyncWorker, Outbox Publisher, Event Bus, Coaching handler, Intelligence handler, Dashboard Coach.

**Secuencia:**

1. Atleta en la PWA toca "Conectar Strava".
2. Frontend redirige a `/oauth/strava/authorize` (TrainingData API), que genera state y redirige a Strava.
3. Atleta autoriza en Strava.
4. Strava redirige al callback `/oauth/strava/callback` con authorization code.
5. TrainingData API:
    - Valida state.
    - Intercambia code por tokens via `StravaOAuthClient`.
    - Crea agregado `AthleteDataSource` con tokens cifrados.
    - Dispara caso de uso `InitialSyncCommand`.
    - Persiste agregado + evento en outbox en misma transacción.
6. `OutboxPublisher` lee outbox, publica `DataSourceConnected` al bus.
7. `SyncWorker` consume `DataSourceConnected` e inicia sync histórico (último 30 días).
8. Para cada actividad fetched de Strava:
    - Mapeo a `CanonicalActivity` via `StravaActivityMapper`.
    - Persistencia de agregado `Activity`.
    - Evento `ActivityIngested` al outbox.
9. `OutboxPublisher` publica `ActivityIngested`.
10. **Coaching handler** consume `ActivityIngested`:
    - Busca `PlannedSession` matcheable por fecha + tipo.
    - Crea o actualiza `SessionExecution`.
    - Emite `SessionExecutionUpdated`.
11. **Intelligence handler** consume `ActivityIngested`:
    - Actualiza métricas agregadas del atleta (TSS, CTL, ATL).
    - Si detecta patrón anómalo, genera sugerencia.
12. En paralelo, atleta ve "Strava conectado ✅" inmediatamente (paso 5 ya completó).
13. Coach, al refrescar dashboard, ve las actividades nuevas y cualquier sugerencia generada.

**Garantías:**
- Si el sync falla a mitad de camino, se retoma por actividad no duplicada.
- Si la API cae después del paso 5, el atleta ve la conexión exitosa pero el sync se completa en segundo plano.
- Si el mapping falla para una actividad, las demás continúan.

## 12.2 Flujo: Generación y aplicación de sugerencia semanal

**Participantes:** Scheduler, AI Worker, Intelligence Application, LLM (Anthropic), Coaching API, Dashboard Coach.

**Secuencia:**

1. Todos los lunes a las 06:00 UTC-3, un scheduled job enqueue `GenerateWeeklyReviewCommand` por cada coach activo.
2. `AI Worker` consume los comandos uno a uno.
3. Para cada coach, el worker:
    - Obtiene la lista de atletas activos.
    - Para cada atleta: construye contexto (plan actual, últimas 2-4 semanas de ejecución, readiness snapshots, objetivos, historial de lesiones).
    - Llama a `IInsightGenerator` (implementación Anthropic) con prompt estructurado + contexto.
    - LLM retorna JSON estructurado con sugerencias (o "sin acción necesaria").
    - Validación de JSON contra schema; si falla, reintento con feedback.
    - Crea agregados `CoachSuggestion` uno por recomendación.
    - Persiste + outbox `CoachSuggestionGenerated`.
4. Communication handler consume `CoachSuggestionGenerated` y envía notificación al coach.
5. Lunes 9:00 AM, coach abre dashboard.
6. Dashboard llama `GetCoachDashboardQuery` que retorna atletas priorizados + sugerencias pendientes.
7. Coach tap "Aceptar" en una sugerencia.
8. Frontend llama `ApplyCoachSuggestionCommand` en Coaching API.
9. Coaching API:
    - Carga `CoachSuggestion` via public API de Intelligence.
    - Carga `TrainingPlan` del atleta.
    - Aplica los cambios sugeridos al plan (nueva versión).
    - Persiste + outbox `CoachSuggestionAccepted` + `TrainingPlanAdjusted`.
10. Intelligence handler consume `CoachSuggestionAccepted` y actualiza `CoachStyleProfile` (aprende).
11. Communication handler consume `TrainingPlanAdjusted` y notifica al atleta.

## 12.3 Flujo: Atleta registra feedback subjetivo de sesión

1. Atleta completa corrida (Garmin la sube a Strava → ya la tenemos en el sistema por flujo 12.1).
2. Atleta en la PWA ve "¿Cómo fue la sesión?" con controles rápidos (RPE 1-10, ánimo, sueño anoche).
3. Atleta completa, frontend llama `RecordSessionFeedbackCommand` en Coaching API.
4. Coaching API carga `SessionExecution`, agrega feedback, persiste + emite `SessionExecutionUpdated`.
5. Intelligence handler consume y ajusta readiness del día si aplica.
6. Si RPE muy alto + deviación grande → Intelligence genera sugerencia inmediata (no espera al lunes).

## 12.4 Flujo: Recuperación de token OAuth revocado

1. `SyncWorker` intenta sync, Strava responde `401 Unauthorized`.
2. Worker verifica si es token expirado → intenta refresh. Si funciona, continúa.
3. Si el refresh también falla (revocado), worker marca `AthleteDataSource` como `Disconnected` con razón `ExternalRevocation`.
4. Emite `DataSourceDisconnected`.
5. Communication notifica al atleta: "Tu conexión con Strava se perdió. Reconectá tocando acá.".

---

# Nivel 13 — Clean Architecture interna y tecnologías

## 13.1 Estructura interna de un módulo (ejemplo: Coaching)

```
Coaching.Domain/
├── Aggregates/
│   ├── TrainingPlanAggregate/
│   │   ├── TrainingPlan.cs                # AggregateRoot
│   │   ├── TrainingPlanId.cs              # strongly-typed ID
│   │   ├── TrainingWeek.cs                # Entity
│   │   ├── PlannedSession.cs              # Entity
│   │   ├── SessionType.cs                 # Value Object
│   │   ├── Intensity.cs                   # Value Object
│   │   └── Events/
│   │       ├── TrainingPlanCreated.cs
│   │       └── TrainingPlanAdjusted.cs
│   └── SessionExecutionAggregate/
│       └── ...
├── DomainServices/
│   └── LoadProgressionPolicy.cs           # lógica que no pertenece a un agregado
├── Repositories/
│   ├── ITrainingPlanRepository.cs         # solo interfaces
│   └── ISessionExecutionRepository.cs
├── Exceptions/
│   ├── InvalidPlanAdjustmentException.cs
│   └── LoadProgressionViolatedException.cs
└── SeedWork/                              # base classes del módulo si hacen falta
    └── (usualmente vacío si se usa BuildingBlocks.Domain)

Coaching.Application/
├── UseCases/
│   ├── CreateTrainingPlan/
│   │   ├── CreateTrainingPlanCommand.cs
│   │   ├── CreateTrainingPlanHandler.cs
│   │   ├── CreateTrainingPlanValidator.cs
│   │   └── CreateTrainingPlanResult.cs
│   ├── AdjustTrainingWeek/
│   ├── ApplyCoachSuggestion/
│   ├── GetAthleteWeekView/                # query
│   └── GetCoachDashboard/                 # query
├── IntegrationEventHandlers/
│   ├── OnActivityIngestedHandler.cs       # de TrainingData
│   ├── OnAthleteProfileUpdatedHandler.cs  # de AthleteProfile
│   └── OnCoachSuggestionGeneratedHandler.cs
├── Abstractions/                          # puertos hacia infra
│   ├── IIntegrationEventBus.cs
│   ├── IAthleteProfileApi.cs              # public API de otro módulo
│   └── IIntelligenceApi.cs
├── Dtos/                                  # internos, no los de la API
│   ├── AthleteWeekView.cs
│   └── CoachDashboardView.cs
└── Behaviors/                             # MediatR pipeline (opcional si no está en BuildingBlocks)

Coaching.Infrastructure/
├── Persistence/
│   ├── CoachingDbContext.cs
│   ├── Configurations/                    # EF Core entity configs
│   │   ├── TrainingPlanConfiguration.cs
│   │   └── SessionExecutionConfiguration.cs
│   ├── Repositories/
│   │   ├── TrainingPlanRepository.cs
│   │   └── SessionExecutionRepository.cs
│   ├── Outbox/
│   │   └── CoachingOutboxProcessor.cs
│   └── Migrations/
├── IntegrationEvents/
│   └── CoachingIntegrationEventPublisher.cs
└── Startup/
    └── CoachingModuleExtensions.cs        # AddCoachingModule + MapCoachingEndpoints

Coaching.Api/
├── Endpoints/
│   ├── CoachDashboard/
│   │   └── GetCoachDashboardEndpoint.cs
│   ├── TrainingPlans/
│   │   ├── CreateTrainingPlanEndpoint.cs
│   │   └── AdjustTrainingWeekEndpoint.cs
│   └── SessionExecutions/
│       └── RecordFeedbackEndpoint.cs
├── Contracts/                             # request/response de la API, separado de Application DTOs
│   ├── Requests/
│   └── Responses/
├── Mapping/
│   └── CoachingApiMappingProfile.cs
└── Authorization/
    └── CoachAccessRequirement.cs
```

## 13.2 Stack tecnológico consolidado

### Backend
| Capa | Tecnología | Por qué |
|------|------------|---------|
| Lenguaje | C# 12 / .NET 8 | Maduro para DDD, performance top-tier, mercado laboral |
| Web framework | ASP.NET Core Minimal APIs | Moderno, bajo overhead |
| Mediator | MediatR | Estándar de facto para CQRS-light en .NET |
| Validación | FluentValidation | Composable, testeable |
| ORM | EF Core 8 | Maduro, soporta DDD bien con configuraciones |
| Migrations | EF Core Migrations | Versionado, reversible |
| Background jobs | Hangfire | UI incluida, reintentos, scheduled jobs |
| Mapping | Mapperly (source-gen) o manual | Sin reflection, performante |
| Logging | Serilog | Estructurado, sinks ricos |
| Observability | OpenTelemetry (.NET SDK) | Estándar abierto |
| Errors | Sentry.NET | Error tracking contextual |
| Testing | MSTest (mantengo continuidad con curso) + FluentAssertions + Testcontainers + Bogus | Lo que ya uso |
| HTTP client | Refit o HttpClient + DelegatingHandlers | Tipado, testeable |

### Data
| Componente | Tecnología |
|------------|------------|
| DB transaccional | PostgreSQL 16 |
| Time-series | TimescaleDB (extensión) |
| Vectorial | pgvector (extensión) |
| Cache / event bus MVP | Redis 7 |
| Object storage | Cloudflare R2 (S3-compatible, barato) |

### Integraciones externas
| Propósito | Servicio |
|-----------|----------|
| LLM | Anthropic (Claude Sonnet / Haiku según tarea) |
| Wearables | Garmin Connect, Strava, Polar, Suunto, COROS (gradual) |
| Email transaccional | Resend o SendGrid |
| Auth (si se terceriza) | Clerk (opción A) o ASP.NET Core Identity (opción B) |
| Pagos (fase 2) | Stripe + MercadoPago |
| Monitoring | Grafana Cloud free tier o Better Stack |

### Frontend
| Componente | Tecnología |
|------------|------------|
| Framework | React 18 + TypeScript + Vite |
| Routing | TanStack Router |
| Server state | TanStack Query |
| Client state | Zustand |
| Forms | React Hook Form + Zod |
| Styling | Tailwind + shadcn/ui |
| Charts | Recharts o Visx |
| PWA | Vite PWA plugin + Workbox |
| Testing | Vitest + React Testing Library + Playwright (E2E) |

### Monorepo (frontends)
| Herramienta | Uso |
|-------------|-----|
| pnpm workspaces | Gestión de paquetes |
| Turborepo | Orquestación de builds + cache |

### DevOps
| Componente | Tecnología |
|------------|------------|
| Contenedores | Docker |
| CI/CD | GitHub Actions |
| Hosting MVP | Railway o Fly.io |
| IaC (cuando aplique) | Terraform o Pulumi |

## 13.3 Patrones técnicos clave

1. **Outbox pattern:** eventos de integración en tabla `outbox` dentro de la misma transacción que el agregado. Worker publica al bus.
2. **Inbox pattern:** eventos consumidos registrados en tabla `inbox` para idempotencia.
3. **CQRS light:** commands pasan por agregados; queries leen directo a proyecciones/DTOs.
4. **Anticorruption Layer:** una implementación por proveedor externo, traducción a modelo canónico.
5. **Specification pattern:** para queries complejas reutilizables.
6. **Domain events in-process:** despachados por MediatR dentro de la misma unidad de trabajo.
7. **Strongly-typed IDs:** records en Domain, convertidos por EF a columnas de DB.
8. **Result pattern:** errores esperables como valores, no excepciones; excepciones solo para casos realmente excepcionales.
9. **Pipeline behaviors (MediatR):** logging, validation, transaction, authorization aplicados transversalmente.
10. **Feature flags:** control granular de features nuevas en producción.

## 13.4 Estrategia de testing

| Nivel | Herramientas | Cobertura objetivo | Qué testea |
|-------|--------------|---------------------|-------------|
| Unit — Domain | MSTest + FluentAssertions | 90%+ | Invariantes de agregados, value objects, domain services |
| Unit — Application | MSTest + NSubstitute | 70%+ | Casos de uso con mocks |
| Integration | MSTest + Testcontainers (PostgreSQL real) | 60%+ | Repos, handlers de eventos, flujos intra-contexto |
| Contract | MSTest | 100% de integraciones externas | Adapters de proveedores externos |
| E2E | Playwright | 5-10 flujos críticos | Flujos user-facing |
| Performance | k6 o NBomber | Endpoints críticos | Latencia bajo carga |
| Security | OWASP ZAP + Snyk | Automatizado en CI | Vulnerabilidades comunes |

---

# Nivel 14 — Infraestructura, CI/CD y ambientes

## 14.1 Ambientes

| Ambiente | Propósito | Data | Costos |
|----------|-----------|------|--------|
| `local` | Dev en máquina del dev | Docker Compose con fixtures | 0 |
| `dev` | Rama `develop` desplegada, branch previews | Sintética | ~20 USD/mes |
| `staging` | Pre-producción, QA, pruebas de integración | Anonimizada o sintética rica | ~40 USD/mes |
| `prod` | Producción real | Real | Variable |

## 14.2 Infraestructura por ambiente (MVP)

**MVP - Railway o Fly.io:**
- 1 servicio API
- 1-2 servicios Worker
- PostgreSQL gestionado (con TimescaleDB)
- Redis gestionado
- Cloudflare R2 para object storage
- Cloudflare como CDN + WAF gratuito

**Post-tracción - AWS:**
- ECS Fargate para API + workers
- RDS PostgreSQL Multi-AZ con TimescaleDB
- ElastiCache Redis
- S3 + CloudFront
- Secrets Manager
- Route 53 + ACM

## 14.3 Pipeline CI/CD (GitHub Actions)

### Pipeline de PR (en cada push a branch)
1. Checkout.
2. Setup .NET 8.
3. Restore dependencias.
4. Build en modo Release.
5. Lint (dotnet format --verify).
6. Ejecutar unit tests + integration tests con Testcontainers.
7. Cobertura de tests publicada.
8. Dependency scan (Snyk o equivalente).
9. Secret scan (gitleaks).
10. Build Docker image para verificar que buildea.
11. Bloquea merge si algo falla.

### Pipeline de `develop` (después de merge)
1. Todo lo anterior.
2. Build + push de Docker image etiquetada con commit SHA.
3. Deploy automático a ambiente `dev`.
4. Smoke tests post-deploy.

### Pipeline de `main` (después de merge con approval)
1. Todo lo de develop.
2. Deploy a `staging` automático.
3. Aprobación manual gate.
4. Deploy a `prod` con estrategia rolling.
5. Monitoreo post-deploy (5 min) con rollback automático si error rate spike.

### Pipeline nocturno
1. Tests de restore de backup en ambiente efímero.
2. Dependency update check.
3. Performance smoke test.

## 14.4 Estrategia de branches (Gitflow adaptado)

```
main       ────────────────────────●───●──────  (prod)
                                  /   /
release    ───────●───────●─────/───/─────────  (staging fijo)
                 /       /     /   /
develop    ──●──●───●───●─────●───●──────────   (dev)
             \  \    \   \
feature      ●──●    ●    ●   (feature branches)
```

- `main` = prod.
- `develop` = dev ambiente, integración continua.
- `feature/*` = branches de features.
- `release/*` = estabilización pre-prod.
- `hotfix/*` = parches urgentes desde main.

Commits en Conventional Commits en español (como ya venís haciendo).

## 14.5 Observabilidad

### Stack
- **Logs:** Serilog → Grafana Loki o Better Stack Logs.
- **Métricas:** OpenTelemetry → Prometheus + Grafana.
- **Traces:** OpenTelemetry → Tempo o Honeycomb.
- **Errores:** Sentry.
- **Uptime:** Better Uptime o UptimeRobot.

### Dashboards obligatorios
1. **Técnico (para on-call):** latencia p50/p95/p99, error rate, saturación, colas pendientes, DB connections.
2. **Integraciones:** success rate por proveedor, tokens expirados, actividades pendientes.
3. **IA:** latencia de LLM, tokens consumidos, costo por día, tasa de errores de parsing.
4. **Producto (para founder):** MAU/WAU, sugerencias generadas, tasa de aceptación, actividades ingestadas, NPS in-app.

### Alertas (ejemplo)
| Condición | Severidad | Canal |
|-----------|-----------|-------|
| Error rate > 2% en 5 min | High | Push + email |
| Latencia p95 > 3s en 10 min | Medium | Email |
| Cola pendiente > 1000 msgs | High | Push |
| Costo LLM día > umbral | Medium | Email diario |
| Backup nightly failed | Critical | Push inmediato |

## 14.6 Gestión de secretos

- **Nunca en código ni en env vars versionados.**
- **Local dev:** `.env.local` en `.gitignore`, ejemplo en `.env.example`.
- **Cloud:** Secrets Manager (AWS/Doppler/Railway secrets).
- **Rotación automática** trimestral para credenciales críticas.
- **Acceso con menor privilegio** + audit log.

## 14.7 Estrategia de backups

- **DB:** snapshots diarios, retención 30 días; snapshots semanales retenidos 90 días.
- **Test de restore mensual** en ambiente efímero (si nunca restauraste, no tenés backup).
- **Object storage:** versionado habilitado + lifecycle a cold storage tras 30 días.
- **Backups cross-region** para DR (cuando el negocio lo justifique).

---

# Nivel 15 — Roadmap y fases de construcción

## 15.1 Principio guía

**"Vertical slices before horizontal layers."**

No construir "toda la capa de infraestructura", después "toda la capa de dominio". Construir **el flujo más delgado posible que funcione end-to-end**, y después iterar ancheándolo.

## 15.2 Fases propuestas

### Fase 0 — Validación (semanas 1-2)

**Sin código.** El código en esta fase es negativo.

- Entrevistas con 8-10 coaches de endurance reales (Uruguay, Argentina, España).
- Preguntas centradas en dolor, no en solución.
- Documentar en papel: wedge refinado, propuesta de valor, willingness-to-pay.
- Decisión go/no-go informada por los datos recolectados.
- Reclutamiento de 2-3 coaches para beta privada.

**Entregable:** documento de validación con quotes, insights, pricing tentativo.

---

### Fase 1 — Fundaciones técnicas (semanas 3-5)

**Solo plomería, sin feature de producto.**

- Setup del monorepo (backend + frontends).
- Solución .NET con BuildingBlocks.
- Esqueleto de 2 módulos: Identity + TrainingData (el mínimo para que algo funcione end-to-end).
- CI/CD funcionando: PR checks, deploy a dev automático.
- Docker Compose para desarrollo local.
- Observabilidad base (Serilog, Sentry, healthchecks).
- Dashboard del coach vacío pero autenticado.
- PWA del atleta vacía pero autenticada.
- Database con migrations iniciales.
- Tests unitarios y de integración con cobertura real.

**Entregable:** un atleta puede loguearse, conectar Strava (mock para testing), y ver un dashboard vacío. Desplegado en dev.

**Para contar en el portfolio:** "Architected foundation: Clean Architecture, CI/CD, observability, auth — all working before writing a single business feature."

---

### Fase 2 — Core de ingesta y visualización (semanas 6-9)

**Flujo vertical mínimo viable.**

- Integración real con Strava (OAuth + sync de actividades).
- Anticorruption layer para Strava con tests de contrato.
- Outbox pattern funcionando.
- Event bus entre módulos.
- Dashboard del coach muestra lista de atletas con última actividad.
- Vista del atleta en el dashboard con timeline de actividades.
- PWA del atleta muestra sus actividades.

**Entregable:** un coach invita a un atleta, el atleta conecta Strava, sus actividades aparecen en el dashboard del coach. Funcional end-to-end.

**Beta privada 1 empieza:** 1-2 coaches de confianza prueban.

---

### Fase 3 — Planificación (semanas 10-13)

- Módulo Coaching: crear plan, ver plan, editar plan.
- Matcheo automático actividad ↔ sesión planificada.
- SessionExecution con feedback subjetivo del atleta en la PWA.
- Versionado de planes funcionando.
- Biblioteca del coach (templates básicos).
- Notificaciones básicas (web push + email fallback).

**Entregable:** flujo completo de coaching sin IA todavía. Ya es usable como "Excel premium".

**Beta privada 2:** 3-5 coaches, 20-30 atletas reales.

---

### Fase 4 — Inteligencia (semanas 14-17)

- Módulo Intelligence con cálculo de readiness nocturno (sin IA, fórmulas fisiológicas).
- Integración con Anthropic Claude.
- Generación semanal de sugerencias para el coach.
- Dashboard priorizado ("estos 3 atletas necesitan atención").
- Aplicación de sugerencias al plan (con revisión del coach).
- Learning loop: tracking de feedback del coach.

**Entregable:** el "momento mágico" del producto funciona. Coach abre dashboard el lunes y recibe sugerencias accionables.

**Beta pública early-access:** 10-20 coaches pagando early-bird (precio reducido).

---

### Fase 5 — Pulido y expansión de integraciones (semanas 18-22)

- Integración con Garmin Connect (OAuth oficial o library con plan de migración).
- Integración con Polar (si demanda).
- Mejoras UX basadas en feedback.
- Performance tuning: índices, caches, queries pesadas.
- Observabilidad de producción madura.
- Hardening de seguridad (pentesting light).
- Cumplimiento legal: términos, política de privacidad, DPAs.

**Entregable:** producto estable para GA comercial.

---

### Fase 6 — Monetización (semanas 23-26)

- Módulo Billing con Stripe.
- Planes: starter (hasta 10 atletas), pro (hasta 30), enterprise (ilimitado + features).
- Trials de 14 días.
- Webhooks de Stripe.
- Self-service de upgrade/downgrade/cancel.
- Facturación fiscal Uruguay (inicial) + Argentina/España.

**Entregable:** primeros cobros reales. Métricas de negocio reales.

---

### Fase 7+ — Escala y expansión (mes 7+)

- Más proveedores (COROS, Suunto, Wahoo).
- Multi-idioma (portugués BR, inglés).
- Módulo de comunicación enriquecido (chat async, notas de audio).
- Modelo ML propio para predicción de lesiones.
- Features para clubes/equipos.
- App nativa iOS/Android si demanda.
- Marketplace de coaches (los coaches se encuentran con atletas).

## 15.3 Definition of Done por fase

Una fase está terminada cuando:
- Todos los tests pasan en CI.
- Cobertura dentro de targets.
- Deploy a staging exitoso.
- Métricas básicas de negocio visibles en dashboard.
- Documentación actualizada (README + este doc + ADRs).
- Al menos un usuario real probó lo nuevo.

## 15.4 Señales para no pasar a la siguiente fase

- Feature de la fase actual no usada por los beta testers.
- Bugs críticos sin resolver.
- Tech debt creciendo más rápido que el valor agregado.
- Coaches churning sin explicación entendida.

## 15.5 No-goals explícitos en año 1

Para mantener foco, estas cosas **no** se construyen:

- App nativa iOS/Android.
- Video analysis / computer vision.
- Nutrición / meal planning.
- Social feed / comunidad.
- Marketplace.
- Soporte multi-deporte fuera de endurance (fuerza, team sports).
- Onboarding self-service masivo (todo es invite-only en año 1).

---

## Apéndice A — Glosario

| Término | Definición |
|---------|-----------|
| **Coach** | Usuario primario pagante. Entrenador profesional gestionando atletas remotos. |
| **Atleta** | Usuario consumidor. Persona que ejecuta los planes del coach. |
| **Tenant** | Ámbito de aislamiento. Un coach (o una organización) = un tenant. |
| **Plan de entrenamiento** | Estructura temporal de sesiones asignadas a un atleta. |
| **Sesión planificada** | Sesión que el coach prescribió. |
| **Sesión ejecutada** | Lo que el atleta realmente hizo, derivado de una actividad. |
| **Actividad** | Registro de un entrenamiento desde un wearable. |
| **Readiness** | Score diario de preparación del atleta para entrenar. |
| **TSS / CTL / ATL / TSB** | Training Stress Score / Chronic / Acute Training Load / Training Stress Balance. Métricas estándar de carga. |
| **HRV** | Heart Rate Variability. Variabilidad de frecuencia cardíaca, indicador de recuperación. |
| **Sugerencia** | Recomendación generada por IA para que el coach evalúe. Nunca se aplica sola. |
| **Bounded context** | Subdominio con lenguaje y modelo propio (DDD). |
| **Agregado** | Cluster de entidades con una raíz que garantiza invariantes (DDD). |
| **Outbox pattern** | Patrón para consistencia entre DB y eventos publicados. |
| **ACL** | Anticorruption Layer. Capa que traduce modelos externos a modelo interno. |

## Apéndice B — Referencias técnicas recomendadas

**DDD y arquitectura:**
- "Implementing Domain-Driven Design" — Vaughn Vernon.
- "Domain-Driven Design Distilled" — Vaughn Vernon.
- "Learning Domain-Driven Design" — Vlad Khononov.
- Eventstorming.com — Alberto Brandolini.

**Clean Architecture:**
- "Clean Architecture" — Robert C. Martin.
- ".NET Microservices: Architecture for Containerized .NET Applications" — Microsoft (libro gratuito).

**Fisiología del deporte (crítico para el dominio):**
- "The Science of Running" — Steve Magness.
- "Training and Racing with a Power Meter" — Hunter Allen, Andrew Coggan.
- "Faster Road Racing" — Pete Pfitzinger.

**LLMs en producción:**
- "Designing Machine Learning Systems" — Chip Huyen.
- Documentación de Anthropic sobre prompt engineering y structured outputs.

## Apéndice C — Próximos pasos inmediatos tras leer este documento

1. Crear repositorio vacío con este documento como `/docs/ARCHITECTURE.md`.
2. Abrir `/docs/adr/` y empezar a registrar ADRs (primer ADR: "Por qué monolito modular").
3. Iniciar Fase 0: agendar 3 entrevistas con coaches para esta semana.
4. En paralelo: setup del monorepo mínimo (paso 1 de Fase 1) para calentar motores.
5. Elegir nombre placeholder y comprar dominio barato (~15 USD/año) para no bloquearse con branding.

---

**Fin del documento.**

*Este es un documento vivo. Cada decisión importante se refleja acá o en un ADR asociado. Si algo cambia en el producto o en el dominio, acá se actualiza primero.*
