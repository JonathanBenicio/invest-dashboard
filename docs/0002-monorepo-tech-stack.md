# 2. Definição da Stack Tecnológica e Abordagem Monorepo

* **Status:** Aceito
* **Data:** 2026-05-18

## Contexto e Definição do Problema

O Invest Dashboard precisa de uma interface amigável e responsiva (capaz de rodar em mobile e web) e de um backend de altíssima performance para lidar com simulações, consolidações financeiras complexas e atualizações de mercado em tempo real. Além disso, precisamos gerenciar ambos no mesmo ciclo de vida de controle de versão.

## Decisão

Decidimos organizar o projeto como um **Monorepo** e utilizar a seguinte Stack:

1. **Frontend:** React 19 + Vite com TypeScript. Para mobile, integração com Capacitor. A gerência de estado será feita com Zustand + TanStack Query para sincronização remota via API.
2. **Backend:** .NET 10 e EF Core estruturados em Domain-Driven Design (DDD). O .NET foi escolhido pela performance e segurança nos cálculos matemáticos/financeiros requeridos pelas simulações.
3. **Banco de Dados/Auth:** Supabase (PostgreSQL + Auth + Storage), devido à excelente infraestrutura de identidade e escalabilidade via Docker.
4. **Comunicação Tempo Real:** ASP.NET Core SignalR para streaming de cotações com baixa latência.

## Consequências

### Positivas
* Experiência fluída do usuário com a reatividade do React 19 e performance matemática escalável no backend com C# 14 (.NET 10).
* O Supabase reduz a carga de desenvolvimento com o gerenciamento nativo e seguro das contas de usuário e JWTs.
* Ter front e back no mesmo repositório sincroniza PRs de ponta a ponta (Fullstack).

### Negativas / Trade-offs
* Aumento no tamanho do repositório local e complexidade em pipelines de CI/CD que precisarão discernir se testam front, back ou ambos baseados nos diffs.
* Curva de aprendizado dupla exigindo conhecimento avançado em TypeScript e C# ao mesmo tempo.