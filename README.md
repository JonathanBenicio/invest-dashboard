# Invest Dashboard - Monorepo

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Build Status](https://github.com/JonathanBenicio/invest-dashboard/actions/workflows/deploy.yml/badge.svg)](https://github.com/JonathanBenicio/invest-dashboard/actions)
[![Node Version](https://img.shields.io/badge/node-%3E%3D%2018-brightgreen)]
[![.NET Version](https://img.shields.io/badge/.NET-10-violet)]

## 📖 Overview
A modern, responsive investment platform built as a Monorepo. It features a state-of-the-art **React 19** and **Vite** frontend (with full mobile support via **Capacitor**) and a robust **.NET 10** backend structured around **Domain-Driven Design (DDD)** using **Entity Framework Core**, **Supabase/Postgres**, and **SignalR** for ultra-low latency real-time market data.

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
- **Security:** JWT Signature validation integrated with Supabase Auth.
- **Storage:** Supabase Storage (Brokerage notes and reports upload with automatic Base64 DB fallback).

---

## 🐳 Running with Docker Compose (Database + API + Frontend)

Launch the entire ecosystem with a single command:

```sh
# Start Postgres, .NET 10 API, and React Frontend simultaneously
docker-compose up -d --build
```

- **Vite Frontend:** `http://localhost:8080`
- **ASP.NET Core Web API:** `http://localhost:5000`
- **PostgreSQL Database:** `localhost:5432`

---

## 🚀 Getting Started (Local Development)

### 📁 Frontend Setup
```sh
cd frontend
npm install
npm run dev
```
Open `http://localhost:5173` to view the local frontend dev server.

### 📁 Backend Setup
```sh
# Ensure you have .NET 10 SDK installed
cd src
dotnet restore
dotnet build InvestDashboard.slnx
dotnet run --project InvestDashboard.WebAPI
```
The API is available locally at `http://localhost:5000` (or `https://localhost:5001`).

---

## 📂 Repository Layout

```plaintext
├── .agents/                 # AI specializations, rules, and checklist scripts
├── frontend/                # Frontend Vite + React + Capacitor App
│   ├── src/                 # React source code
│   ├── public/              # Static assets
│   ├── android/             # Android native studio project
│   ├── package.json         # Node dependencies
│   └── vite.config.ts       # Vite bundler configuration
│
└── src/                     # Backend Solution Folder (.NET 10)
    ├── InvestDashboard.slnx # Modern XML-based .NET Solution file
    ├── InvestDashboard.Domain/ # DDD Domain Layer (Entities, Value Objects, Aggregates)
    ├── InvestDashboard.Application/ # Use Cases & DTOs
    ├── InvestDashboard.Infrastructure/ # EF Core, Supabase integrations, SignalR Hubs, Workers
    ├── InvestDashboard.WebAPI/ # ASP.NET Core Controllers & Minimal APIs
    └── tests/               # Unit and Integration test suites
```

---

## 🤝 Contributing
1. Fork the repository.
2. Create a feature branch.
3. Run lint runners and check tests.
4. Open a Pull Request.

---

## 📄 License
This project is licensed under the MIT License – see the [LICENSE](LICENSE) file.
