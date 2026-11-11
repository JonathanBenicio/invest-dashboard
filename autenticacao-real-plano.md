# Plano — homologação de autenticação real

**Iniciativa GitHub:** https://github.com/JonathanBenicio/invest-dashboard/issues/48

**Situação:** planejamento; convite real entregue e link confirmado na issue #46. Isso não comprova login por senha, refresh, logout ou isolamento entre contas. Referência existente: `docs/plan/supabase-auth-homologation.md`. Base local: branch `codex/supabase-secret-key-migration`, commit `84241c7`; confrontar com develop antes de implementar.

**Objetivo:** homologar o fluxo real Supabase → API → sessão local em ambiente controlado, sem registrar dados pessoais ou segredos.

- [ ] Preparar duas identidades de teste confirmadas e grupos/dados descartáveis; conferir ambiente, redirect permitido e configuração sem divulgar valores. Não alterar configurações remotas sem necessidade identificada.
- [ ] Executar login válido e inválido, conta não confirmada e indisponibilidade; confirmar `/me`, claims e mensagens sem vazamento. Registrar resultados sanitizados.
- [ ] Executar `bun run test:api --grep "Supabase Auth live smoke"` com credenciais em `frontend/.env.e2e.local` ignorado. Confirmar rotação, rejeição do refresh anterior, logout e revogação; verificar cookies e expiração em HTTPS quando houver ambiente apropriado.
- [ ] Validar duas identidades: isolamento de grupos, carteiras e posições, incluindo tentativas de leitura e mutação não autorizadas; observar estados e persistência.
- [ ] Investigar e corrigir separadamente qualquer falha; definir relação entre sessão local e sessão Supabase upstream, inclusive expiração e revogação, com ADR proposta se houver mudança duradoura.
- [ ] Consolidar relatório com ambiente, versão, evidência sanitizada, cobertura e pendências; concluir somente quando todos os critérios forem comprovados. Convite #46 é evidência limitada, não teste integral de autenticação.

**Dependências:** contas de teste controladas, credenciais locais fora do Git, frontend/backend executáveis e ambiente HTTPS para cookies seguros. Não criar nem enviar credenciais em issues. ADR: pendente de análise; User Story: US-AUTH-001 em `docs/USER-STORIES.md`; relatório e PR: pendentes.
