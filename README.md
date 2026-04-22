# AthleteOS

> Plataforma SaaS de coaching inteligente para deportes de endurance.
> Work in progress. Nombre de trabajo, sujeto a cambios.

## Qué es esto

Plataforma B2B que ayuda a coaches de running, ciclismo, triatlón y natación a gestionar atletas a distancia con asistencia de inteligencia artificial. Ingiere datos de wearables (Strava, Garmin, Polar), los analiza continuamente, y presenta al coach un dashboard priorizado con sugerencias accionables que el coach aprueba, modifica o rechaza.

**Usuario principal:** el coach. El atleta usa la app como consumidor, no como cliente pagante.

**Estado actual:** Fase 1 — Fundaciones técnicas. Sin features de producto todavía.

## Documentación

Toda la documentación técnica vive en el repo:

| Archivo | Qué contiene |
|---------|--------------|
| [`CLAUDE.md`](./CLAUDE.md) | Briefing operativo para agentes de IA (Claude Code). |
| [`docs/ARCHITECTURE.md`](./docs/ARCHITECTURE.md) | Documento de arquitectura completo, 15 niveles. |
| [`docs/adr/`](./docs/adr/) | Architecture Decision Records — decisiones formales. |
| [`specs/`](./specs/) | Specs de features (Spec-Driven Development). |

**Por dónde empezar:**

- Si sos un humano nuevo en el proyecto: leé este README, después `docs/ARCHITECTURE.md` niveles 1-5.
- Si sos un agente de IA: leé `CLAUDE.md`. Te redirige a lo que necesitás.
- Si venís a contribuir: leé [CONTRIBUTING.md](./CONTRIBUTING.md) (por escribir).

## Stack técnico (resumen)

**Backend:** C# / .NET 8 + ASP.NET Core + EF Core + PostgreSQL + TimescaleDB + Redis.
**Frontend:** React + TypeScript + Vite + TanStack + Tailwind.
**IA:** Anthropic Claude.
**Infra:** Docker + GitHub Actions + Railway/Fly.io (MVP) → AWS (escala).

Detalle completo en `docs/ARCHITECTURE.md` nivel 13.

## Requisitos para desarrollo local

- **.NET 8 SDK** — [descargar](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Node.js 20+ y pnpm 9+** — recomendado via [Volta](https://volta.sh/) o [fnm](https://github.com/Schniz/fnm)
- **Docker Desktop** (o Docker Engine + Compose plugin en Linux)
- **Git**
- Editor de código: VS Code + C# Dev Kit (gratis), Rider, o similar

## Setup local

### 1. Cloná el repo

```bash
git clone <url-del-repo>
cd athleteos
```

### 2. Copiá el archivo de variables de entorno

```bash
cp .env.example .env
```

Editá `.env` si necesitás personalizar algo. Para desarrollo local, los defaults deberían funcionar.

### 3. Levantá los servicios de infraestructura

```bash
docker compose up -d
```

Esto levanta PostgreSQL (con TimescaleDB), Redis y un MailHog local para testing de emails. Verificá con:

```bash
docker compose ps
```

### 4. Aplicá las migraciones de la base de datos

```bash
# (Disponible cuando exista el Bloque 2)
dotnet run --project src/Bootstrap/ApiHost -- migrate
```

### 5. Corré la API

```bash
# (Disponible cuando exista el Bloque 2)
dotnet run --project src/Bootstrap/ApiHost
```

La API queda corriendo en `http://localhost:5000`. Swagger UI en `http://localhost:5000/swagger`.

### 6. (Opcional) Corré los frontends

```bash
# (Disponible cuando exista el Bloque 4)
cd frontends
pnpm install
pnpm dev
```

El dashboard del coach queda en `http://localhost:5173`, la PWA del atleta en `http://localhost:5174`.

## Estructura del repo

```
athleteos/
├── CLAUDE.md                    Briefing para agentes de IA
├── README.md                    Este archivo
├── .env.example                 Template de variables de entorno
├── .gitignore
├── .editorconfig                Convenciones de edición
├── docker-compose.yml           Servicios de infraestructura local
├── docs/
│   ├── ARCHITECTURE.md          Arquitectura (15 niveles)
│   └── adr/                     Decisiones arquitectónicas
├── specs/                       Specs de features
├── src/                         Código del backend (.NET)
│   ├── BuildingBlocks/
│   ├── Modules/
│   ├── Bootstrap/
│   └── Workers/
├── frontends/                   Monorepo de frontends (por crear)
│   ├── apps/
│   └── packages/
├── tests/                       Tests del backend
└── .github/
    └── workflows/               CI/CD (GitHub Actions)
```

## Cómo correr los tests

```bash
# Backend completo
dotnet test

# Solo unit tests
dotnet test --filter Category=Unit

# Solo un módulo
dotnet test tests/Modules/Coaching.UnitTests

# Frontend (cuando exista)
cd frontends && pnpm test
```

## Comandos útiles del día a día

| Comando | Qué hace |
|---------|----------|
| `docker compose up -d` | Levanta infra local |
| `docker compose down` | Detiene infra local |
| `docker compose logs -f postgres` | Logs de PostgreSQL |
| `dotnet build` | Compila la solución |
| `dotnet test` | Corre todos los tests |
| `dotnet format` | Formatea el código |
| `pnpm dev` | Arranca frontends en modo dev |

## Convenciones

- **Commits:** Conventional Commits en español. Ej: `feat(coaching): agregar ajuste de semana`.
- **Branches:** Gitflow adaptado. `main` es prod, `develop` es dev, features en `feature/*`.
- **Código:** inglés siempre. Comentarios y docs en español o inglés consistente.
- **Tests:** obligatorios para nuevo código en Domain y Application.

## Reglas importantes

1. **Nunca commitear secretos.** Usamos `.env.local` (en `.gitignore`) y vault en cloud.
2. **Nunca mergear directo a `main` ni a `develop`.** Siempre vía PR.
3. **Features nuevas requieren spec antes de código** (ver `docs/adr/0002-sdd-sobre-ddd.md`).
4. **Cambios arquitectónicos requieren ADR.**

## Soporte y contacto

Proyecto en fase temprana, mantenido por [Felipe](https://github.com/<tu-usuario>).

Para bugs, issues o propuestas: abrí un issue en este repo.

## Licencia

Por definir. Hasta tener decisión formal, el código es propietario y no se permite redistribución.
