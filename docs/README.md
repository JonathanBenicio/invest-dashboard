# 📚 Invest Dashboard - Central de Documentação

Bem-vindo ao diretório de documentação do **Invest Dashboard**. Aqui centralizamos todas as decisões técnicas, regras de negócios, e guias de implementação do projeto.

## 📂 Estrutura de Diretórios

- **`/adr/` (Architecture Decision Records)**
  Registros de decisões arquiteturais. Sempre que tomarmos uma decisão técnica importante (escolha de frameworks, padrões de arquitetura, etc.), documentamos aqui o contexto, a decisão e as consequências.

- **`/bdd/` (Behavior-Driven Development)**
  Cenários de testes e comportamentos descritos em formato Gherkin (`Dado que... Quando... Então...`). Usado para alinhar o entendimento das regras de negócio entre desenvolvimento e as `USER-STORIES.md`.

- **`/plan/`**
  Planos de implementação técnicos e de migração para funcionalidades maiores (ex: refatoração de DTOs, novos módulos).

## 📄 Arquivos Principais

- USER-STORIES.md: Lista detalhada de todos os Épicos e Histórias de Usuário da plataforma.
- ADR Template: Use este modelo para criar novos ADRs.

## 🚀 Como Contribuir com a Documentação

1. **Novas Decisões:** Crie um novo arquivo em `/adr/` copiando o `TEMPLATE.md` e nomeando de forma sequencial (ex: `0003-uso-de-redis.md`).
2. **Novas Regras de Negócio:** Adicione ao `USER-STORIES.md` e, se necessário, crie os cenários correspondentes em `.feature` na pasta `/bdd/`.
3. **Padrões de Código:** Se alterar a convenção de nomes ou diretrizes, atualize os respectivos planos em `/plan/`.

---

> **Nota:** Para diretrizes específicas de Agentes de IA e workflows automatizados, consulte a pasta `.agents/` na raiz do projeto.