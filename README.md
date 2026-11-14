# Invest Dashboard - Monorepo

[![Build Status](https://github.com/JonathanBenicio/invest-dashboard/actions/workflows/deploy.yml/badge.svg)](https://github.com/JonathanBenicio/invest-dashboard/actions)
[![Node Version](https://img.shields.io/badge/node-%3E%3D%2018-brightgreen)]
[![.NET Version](https://img.shields.io/badge/.NET-10-violet)]

## 📖 Overview
A monorepo for an investment dashboard with a **React 19** and **Vite** frontend, Capacitor integration, and a **.NET 10** backend using **Entity Framework Core**, PostgreSQL, Supabase Auth, and SignalR. See the [documentation index](./docs/INDEX.md) for implementation status and validation limits.

---

## 🏗️ Monorepo Architecture

The repository is structured into two main independent but cohesive domains:

### 📱 [Frontend (React 19 + Capacitor)](./frontend/)
A beautiful, highly-responsive user experience featuring portfolio insights, asset distribution, and transaction logs.
- **Path:** `frontend/`
- **Core Stack:** React 19, Vite, TypeScript, Tailwind CSS, shadcn-ui, Zustand, TanStack Query, Recharts.
- **Mobile Capabilities:** Integrated with Capacitor for native Android and iOS builds.

### ⚙️ [Backend (.NET 10 + EF Core + DDD)](./src/)
A scalable, decoupled backend engine organized around clean architecture and domain-driven design principles.
- **Path:** `src/`
- **Core Stack:** ASP.NET Core 10, EF Core 10, PostgreSQL (Supabase Postgres Image), SignalR (Realtime Quotations), Background Services (Market Quotation Synchronizers).
- **Security:** Supabase Auth provides identity; the API issues and validates its own JWTs.
- **Storage:** Brokerage-note uploads can use Supabase Storage or return a Base64 data URI, depending on configuration.

---

## 🐳 Running with Docker Compose (Database + API + Frontend)

Copy `.env.example` to `.env` and replace its placeholders before starting the stack. Compose requires these values:

- `POSTGRES_PASSWORD`: a unique local database password.
- `JWT_SECRET`: at least 32 random bytes for this API's own JWTs; it is not a Supabase key. Generate one with `openssl rand -hex 32`.
- `SUPABASE_URL` and `SUPABASE_PUBLISHABLE_KEY`: your Supabase project URL and publishable key (`sb_publishable_...`).
- `SUPABASE_SECRET_KEY`: the server-only Supabase key (`sb_secret_...`) used by the API to send group invitations. Never expose it in frontend code or commit it.
- `SUPABASE_INVITE_REDIRECT_URL`: the invitation redirect URL, which must be allowed in the project's Supabase Auth redirect URL settings. The example points to `http://localhost:8080/convite/aceitar`.

`POSTGRES_DB`, `POSTGRES_USER`, and `POSTGRES_PORT` default to `postgres`, `postgres`, and `5432`. `BRAPI_API_TOKEN` is optional. Start the stack with:

```sh
docker compose up -d --build
```

- Frontend: `http://localhost:8080`
- ASP.NET Core API: `http://localhost:5000`
- PostgreSQL: `localhost:5432` (bound to localhost)

The frontend container proxies `/api` and `/hubs` to the API, so browser requests and the SignalR connection use the same origin. The API migrates the database on startup. Keep `.env` private and do not commit it.

---

## 🚀 Getting Started (Local Development)

### 📁 Frontend Setup
```sh
cd frontend
bun install
bun run dev
```
The Vite configuration requests port `5000`; Vite may choose another port if it is occupied. Check the URL printed at startup. The Compose API also uses port 5000, so running both locally may cause a conflict.

### 📁 Backend Setup
```sh
# Ensure you have .NET 10 SDK installed
cd src
dotnet restore
dotnet build InvestDashboard.slnx
dotnet run --project InvestDashboard.WebAPI
```
Configure `ConnectionStrings__DefaultConnection`, `Jwt__Secret` (at least 32 random bytes), `Storage__SupabaseUrl`, and `Storage__SupabaseApiKey` in the process environment before starting the API. To send group invitations, also configure `Storage__SupabaseSecretKey` and `Storage__SupabaseInviteRedirectUrl`; keep the secret key on the server. These values are intentionally not stored in appsettings files.

---

## 📂 Repository Layout

```plaintext
├── .agents/                 # Shared project skills; other local legacy material may be ignored
├── frontend/                # Frontend Vite + React + Capacitor App
│   ├── src/                 # React source code
│   ├── public/              # Static assets
│   ├── android/             # Generated, Git-ignored native project when created
│   ├── package.json         # Node dependencies
│   └── vite.config.ts       # Vite bundler configuration
│
└── src/                     # Backend Solution Folder (.NET 10)
    ├── InvestDashboard.slnx # Modern XML-based .NET Solution file
    ├── InvestDashboard.Domain/ # DDD Domain Layer (Entities, Value Objects, Aggregates)
    ├── InvestDashboard.Application/ # Use Cases & DTOs
    ├── InvestDashboard.Infrastructure/ # EF Core, Supabase integrations, SignalR Hubs, Workers
    ├── InvestDashboard.WebAPI/ # ASP.NET Core controllers
    └── tests/               # Unit and Integration test suites
```

---

## 🤝 Contributing
1. Fork the repository.
2. Create a feature branch.
3. Run lint runners and check tests.
4. Open a Pull Request.

### Documentation and traceability

For the documentation and implementation workflow, templates, and repository indices, see:

- [Contribution workflow](./conductor/workflow.md)
- [Documentation index](./docs/INDEX.md)
- [User Stories catalog](./docs/USER-STORIES.md)
- [Templates](./templates/README.md)

Use the Issue → ADR → User Story → Plan → Implementation → Validation → PR flow where applicable. The workflow defines when an artifact can be N/A and how to record evidence and gaps.

---

## 📄 License
No license file is present in this checkout. The project license has not been confirmed.
