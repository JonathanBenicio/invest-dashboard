# GEMINI.md - Global Orchestrator

Este é o arquivo central de instruções para o agente Gemini neste workspace. Para o processo documental compartilhado, consulte `conductor/workflow.md`; referências ao material `.agents` podem depender de arquivos locais ignorados pelo Git.

---

## 🏗️ Estrutura do Projeto & Roteamento de Regras

O projeto é um monorepo dividido em Frontend e Backend. As regras específicas de cada domínio estão localizadas em seus respectivos diretórios:

- As instruções compartilhadas de frontend e backend estão em `frontend/AGENTS.md` e `src/AGENTS.md`. Arquivos `GEMINI.md` por área não estão presentes neste checkout.

### 🔴 REGRA DE OURO
Consulte as instruções aplicáveis à área alterada e respeite as regras e permissões da sessão.

---

## 🤖 Sistema de Agentes & Skills

Parte do material legado está em `.agents/`, mas nem todos os arquivos locais são rastreados pelo Git. Consulte `docs/codex/codex-migration.md` para limites e localização da configuração compartilhada.

### Instruções para Uso de Agentes
1. **Identificação**: Determine se a tarefa é de Frontend, Backend ou Geral.
2. **Ativação**: Leia o arquivo do agente em `.agents/agents/{agent-name}.md`.
3. **Skills**: Carregue as skills listadas no frontmatter do agente a partir de `.agents/skills/`.

---

## 🧹 Regras Globais (TIER 0)

### Limpeza & Qualidade
- **Clean Code**: Siga rigorosamente a skill `@[skills/clean-code]`.
- **Lint & Types**: Execute os comandos de verificação (ex: lint, type-check, build) antes de considerar uma tarefa concluída. **Zero tolerância para erros.**

### 📜 Governança de Documentação (Roadmap Q2 2026)
Para trabalho planejado, aplique o fluxo e as condições de dispensa de `conductor/workflow.md`:
1. **GitHub Issue**: Quando aplicável, registrar a necessidade e os links dos artefatos.
2. **ADR (Architectural Decision Record)**: Quando aplicável, registrar decisões em `docs/adrs/`.
3. **User Story**: Detalhamento dos critérios de aceite em `docs/USER-STORIES.md`.
4. **Implementation Plan**: Quando aplicável, roteiro técnico em `docs/plan/`.
5. **Rastreabilidade**: Use `Refs #ID` para trabalho parcial e `Closes #ID` somente quando os critérios forem atendidos e comprovados.
6. **Sincronização de índices**: Atualizar `docs/INDEX.md` e demais índices pertinentes conforme `conductor/workflow.md`; `CONSOLIDATED_DOCS.md` não é exigido quando inexistente.

### Comunicação
- Responda no idioma do usuário.
- Mantenha comentários e nomes de variáveis em Inglês, a menos que existam termos de domínio específicos já estabelecidos no projeto.

---

## 📁 Referência Rápida
- **Workflows**: `.agents/workflows/`
- **Scripts de Verificação**: `.agents/scripts/verify_all.py`
- **Documentação de Arquitetura**: `.agents/ARCHITECTURE.md`
