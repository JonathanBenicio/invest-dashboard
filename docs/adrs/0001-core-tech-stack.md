# ADR 0001: Core Tech Stack and Architecture Pattern

## Status
Accepted

## Context
O "Invest Dashboard" exige uma arquitetura robusta, escalável e de alta responsividade para lidar com dados de mercado em tempo real, autenticação segura e cálculos financeiros complexos. O sistema deve suportar interfaces web e mobile nativas (iOS/Android).

## Decision
Decidimos adotar uma estratégia de Monorepo com a seguinte stack técnica:
1.  **Frontend:** React 19 + Vite + TypeScript. Capacitor é utilizado para compilações nativas mobile.
2.  **Backend:** .NET 10 + Entity Framework Core 10.
3.  **Arquitetura:** Domain-Driven Design (DDD) no backend para isolar a lógica central financeira da infraestrutura.
4.  **Banco de Dados & Auth:** Supabase (PostgreSQL) para armazenamento relacional, Row Level Security (RLS) e Autenticação baseada em JWT.
5.  **Tempo Real:** SignalR para envio de cotações ao vivo e atualizações de background services para os clientes conectados.

## Consequences
- **Positivas:** Alta performance, tipagem forte ponta a ponta (TypeScript/C#), reaproveitamento de código e deploy rápido mobile via Capacitor.
- **Negativas:** Maior curva de aprendizado inicial devido ao padrão DDD; o gerenciamento de ambientes locais vs remotos do Supabase requer orquestração cuidadosa via Docker Compose.
