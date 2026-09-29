# Plano: remover mocks de produto e integrar frontend/backend

Atualizado em 29/09/2026, a pedido do usuário antes de retomar mudanças de código. A branch está no commit `aa31fc9`; o worktree contém alterações frontend ainda não commitadas para análise real, importação CSV e remoção dos fixtures antigos.

## Objetivo e limites

Conectar as telas do produto aos contratos reais de API e eliminar valores simulados apresentados como dados do usuário. A ordem começa pelo acompanhamento de posições. Fluxo de caixa, proventos e pagamentos permanecem fora do escopo até a etapa posterior explicitamente definida pelo usuário.

O MSW pode continuar como ferramenta de testes e demonstração local. Ele não pode ser ativado por padrão, no deploy, nem mascarar uma falha da API como se fosse dado real. Os fixtures de teste devem ser identificáveis como dados de teste.

## Estado encontrado

- O backend já expõe carteiras, posições, transações, histórico patrimonial, cotações, taxas econômicas e simulações.
- Dashboard, carteiras, telas de renda variável/fixa, detalhes de posição, taxas e simulador já usam alguns desses endpoints; a conexão real ainda precisa de verificação integrada.
- `Analysis.tsx` continha séries estáticas de carteira, Ibovespa, CDI, setores e proventos. A implementação preliminar passou a consumir carteira, posições e histórico do backend, calcula alocação/resultados das posições abertas e foi adicionada à navegação. Benchmark continua indisponível sem fonte real.
- `Import.tsx` simulava leitura, criava linhas fixas e declarava importação concluída sem chamar API. A implementação preliminar lê CSV delimitado, valida compra/venda e dados completos de renda fixa, envia operações sequencialmente ao ledger, mostra resultados e permite retry seguro para falhas transitórias.
- `FixedIncomeProjection.tsx` era código sem rota/uso conhecido e projetava rendimentos com CDI/IPCA fixos; foi removido no worktree preliminar.
- `frontend/src/lib/mock-data.ts` fornecia fixtures e também formatação. Os consumidores ativos de formatação foram migrados para `lib/utils.ts`; o arquivo foi removido no worktree preliminar.
- `frontend/src/mocks/handlers.ts` permanece usado pelo Playwright e por uma opção explícita de demonstração. Produção, Docker e GitHub Pages definem `VITE_USE_MSW=false`; o Playwright define `true`.
- As rotas de usuários administrativos, chat e integrações diretas com corretoras não possuem contratos backend correspondentes ou não estão expostas na navegação. Não podem ser apresentadas como funcionais sem implementação real.
- O backend retorna lista vazia para proventos. A UI preliminar removeu a aba de proventos da tela de posições e o fixture MSW deixa de inventar pagamentos.
- A CI de `df2bae7` falhou no backend: frontend e `docker-smoke` passaram, enquanto dois testes aplicavam migrations concorrentes no mesmo banco PostgreSQL compartilhado. A falha reproduzida foi SQLSTATE `42701`, coluna `portfolio_id` já existente na tabela `transactions`. O commit `aa31fc9` cria um database efêmero por teste PostgreSQL; os três jobs da CI passaram.
- Depois dessas alterações locais: typecheck, build e ESLint direcionado passaram (3 avisos preexistentes em `router.tsx`); 5 E2E passaram, incluindo análise pela API e importação de renda variável/renda fixa com uma linha inválida. O lint global conhecido falha com 30 erros e 16 avisos em arquivos legados/gerados.

## Fases de implementação

### Fase 0 — revisar e estabilizar o worktree atual

1. Revisar o diff completo e confirmar que os arquivos apagados estavam sem uso e que nenhuma fixture foi removida do Playwright por engano.
2. [x] Isolar cada teste PostgreSQL em um banco efêmero próprio; a suíte local passou sem skips e a CI de `aa31fc9` passou nos três jobs.
3. [x] Validar as rotas `/analise` e `/importar`, acesso autenticado, consultas e erros; 5 E2E passaram usando fixtures MSW explícitas.
4. [x] Executar typecheck, build e lint direcionado; o lint global mantém falhas legadas documentadas.
5. Fazer commits contextuais da implementação frontend preliminar e executar CI para confirmar a integração completa. O E2E atual usa MSW para a resposta HTTP de importação; ainda falta teste integrado da UI CSV contra API/PostgreSQL e validar Brapi/Supabase reais.
6. Atualizar este plano e o catálogo de user stories com as evidências. Manter o deploy em produção fora desta fase.

**Aceite:** os testes demonstram cotação com origem/horário, aviso quando a API não retorna preço, análise baseada na carteira selecionada e importação de linhas CSV para o ledger real. Nenhuma tela apresenta uma operação simulada como concluída.

### Fase 1 — acompanhamento de posições e cotações

1. Buscar cotações pelo endpoint `/api/v1/market-data/quotes` ao abrir posições e detalhes; escolher a observação mais recente entre resposta HTTP e SignalR.
2. Exibir origem e horário da observação. Quando não houver cotação, manter o último preço persistido e mostrar indisponibilidade.
3. Validar Brapi em homologação configurada, dados incompletos, timeout, símbolos sem resultado, preço inválido, evento antigo e ambiente sem token.
4. Confirmar que não há simulação ou random walk sendo exibido como cotação real.

**Aceite:** a UI mantém quantidade/custo vindo do backend, atualiza valor corrente com cotações reais e identifica preço/origem/horário sem ocultar falhas.

### Fase 2 — análise patrimonial sem séries fixas

1. Carregar lista/detalhe de carteiras e histórico por período pelos endpoints existentes.
2. Calcular distribuição e melhor/pior posição a partir das posições atuais e da cotação mais recente.
3. Exibir apenas pontos históricos completos; indicar lacunas e estados de carregamento/erro.
4. Não plotar benchmarks de CDI/Ibovespa até existir uma fonte/contrato confiável para eles. Não reintroduzir proventos nesta tela.

**Aceite:** nenhum número ou série de `mock-data.ts` participa da análise; a carteira selecionada determina os resultados apresentados.

### Fase 3 — importação de operações pelo ledger

1. Documentar o formato CSV e manter o download do modelo sincronizado com o parser.
2. Validar cabeçalho, separadores, aspas, números BR, data, classe/subtipo, renda fixa, quantidades e taxas antes do envio.
3. Enviar operações sequencialmente pela API autenticada, usando chave de idempotência por linha.
4. Apresentar sucesso/falha por linha e permitir reenvio seguro das falhas sem duplicar linhas já registradas.
5. Cobrir compra, venda parcial, ativo inexistente em venda, sobre-venda, renda fixa incompleta, repetição/idempotência, arquivo vazio/malformado e carteira inválida.

**Aceite:** o CSV não produz sucesso simulado; o resultado refletido no frontend corresponde ao ledger persistido no backend.

### Fase 4 — remover caminhos de dados simulados expostos

1. Buscar todos os imports de fixture em `frontend/src/pages`, `components` e `hooks`, incluindo usos indiretos de valores de exemplo.
2. Para cada página roteada, ligar a API existente; quando não houver endpoint, remover a promessa de funcionalidade e manter estado de indisponibilidade explícito.
3. Manter os handlers MSW apenas como fixtures de teste/demo opt-in. Não ativar MSW nos workflows de build, deploy, Docker ou runtime normal.
4. Revisar chat, usuários administrativos e corretoras: criar backend seguro apenas se fizerem parte do escopo acordado; até lá, manter fora da navegação e não exibir confirmação fictícia.

**Aceite:** uma auditoria de imports/rotas não encontra dataset de negócio hardcoded sendo usado por uma tela de produto; `VITE_USE_MSW=false` em todos os ambientes de produção.

### Fase 5 — conexão das capacidades backend existentes

1. Verificar taxas: CRUD autenticado, valores observados/data/fonte e comportamento sem taxas cadastradas.
2. Verificar simulador: request e response reais, validação de limites, mensagens de erro e atualização da UI.
3. Não usar taxa codificada ou resposta do MSW como taxa corrente em cálculos apresentados como reais; identificar taxa fornecida manualmente pelo usuário.
4. Confirmar testes de autorização e isolamento multiusuário no PostgreSQL.

**Aceite:** cada tela de negócio roteada possui contrato explícito, estado de erro real e teste de integração que cobre a conexão relevante.

### Fase 6 — homologação

1. Configurar em ambiente de homologação URL/API, Supabase e Brapi via secrets do ambiente, sem versionar credenciais.
2. Verificar login/refresh, isolamento de carteiras, criação de transação, cotações/histórico e comportamento de timeout.
3. Publicar evidências de CI e checklist de deploy/rollback. Produção exige solicitação própria.

**Aceite:** fluxos reais aprovados em homologação com dados de teste controlados; nenhuma confirmação baseada apenas em MSW.

## Validação e acompanhamento

- Local já passou nesta continuação: typecheck, build, lint direcionado (3 avisos existentes no router) e 5 testes E2E incluindo análise e importação CSV.
- Testes .NET locais com PostgreSQL: 21 unitários e 27 integrações passaram, sem skips; cada teste PostgreSQL criou seu próprio database, e todos os databases temporários foram removidos.
- CI de `a9ad5ac` passou. A CI de `df2bae7` ([execução 36538233870](https://github.com/JonathanBenicio/invest-dashboard/actions/runs/36538233870)) teve frontend/Docker smoke verdes e falhou no backend por migrations concorrentes. O corretivo `aa31fc9` passou backend/PostgreSQL, frontend e Docker smoke; análise e importação ainda aguardam CI própria.
- Após congelar cada fase, executar a validação correspondente e só então registrar seu aceite neste arquivo e em `docs/USER-STORIES.md`.

## Fora desta entrega

Fluxo de caixa, pagamentos de proventos e apuração fiscal completa. Esses itens exigem seus próprios contratos, critérios e validações; não devem ser substituídos por mocks na fase de posições.
