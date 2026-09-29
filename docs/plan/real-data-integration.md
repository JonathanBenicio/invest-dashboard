# Plano: remover mocks de produto e integrar frontend/backend

Atualizado em 29/09/2026. A implementação de análise real, importação CSV e remoção dos fixtures de produto foi commitada em `605bbc5`; backend/PostgreSQL, frontend e Docker smoke passaram na CI. O E2E de importação pelo navegador contra API/PostgreSQL reais passou localmente sem MSW; apenas a sessão de autenticação é controlada pelo teste. A CI do commit `9066682` (https://github.com/JonathanBenicio/invest-dashboard/actions/runs/36544411654) passou nos quatro jobs. Homologação com credenciais Supabase/Brapi continua pendente.

## Objetivo e limites

Conectar as telas do produto aos contratos reais de API e eliminar valores simulados apresentados como dados do usuário. A ordem começa pelo acompanhamento de posições. Fluxo de caixa, proventos e pagamentos permanecem fora do escopo até a etapa posterior explicitamente definida pelo usuário.

O MSW pode continuar como ferramenta de testes e demonstração local. Ele não pode ser ativado por padrão, no deploy, nem mascarar uma falha da API como se fosse dado real. Os fixtures de teste devem ser identificáveis como dados de teste.

## Estado encontrado

- O backend já expõe carteiras, posições, transações, histórico patrimonial, cotações, taxas econômicas e simulações.
- Dashboard, carteiras, telas de renda variável/fixa, detalhes de posição, taxas e simulador já usam alguns desses endpoints; a conexão real ainda precisa de verificação integrada.
- `Analysis.tsx` consumia séries estáticas de carteira, Ibovespa, CDI, setores e proventos. Em `605bbc5`, passou a consumir carteira, posições e histórico do backend, calcula alocação/resultados das posições abertas e está na navegação. Benchmark continua indisponível sem fonte real.
- `Import.tsx` simulava leitura e conclusão sem chamar API. Em `605bbc5`, lê CSV delimitado, valida compra/venda e dados de renda fixa, envia operações sequencialmente ao ledger, mostra resultados por linha e permite retry seguro para falhas transitórias.
- `FixedIncomeProjection.tsx` era código sem rota/uso conhecido e projetava rendimentos com CDI/IPCA fixos; foi removido em `605bbc5`.
- `frontend/src/lib/mock-data.ts` fornecia fixtures e formatação. Os consumidores ativos de formatação foram migrados para `lib/utils.ts` e o arquivo foi removido em `605bbc5`.
- `frontend/src/mocks/handlers.ts` permanece usado pelo Playwright e por uma opção explícita de demonstração. Produção, Docker e GitHub Pages definem `VITE_USE_MSW=false`; o Playwright define `true`.
- As rotas de usuários administrativos, chat e integrações diretas com corretoras não possuem contratos backend correspondentes ou não estão expostas na navegação. Não podem ser apresentadas como funcionais sem implementação real.
- O backend retorna lista vazia para proventos. A UI removeu a aba de proventos da tela de posições e o fixture MSW deixa de inventar pagamentos.
- A CI de `df2bae7` falhou no backend: frontend e `docker-smoke` passaram, enquanto dois testes aplicavam migrations concorrentes no mesmo banco PostgreSQL compartilhado. A falha reproduzida foi SQLSTATE `42701`, coluna `portfolio_id` já existente na tabela `transactions`. O commit `aa31fc9` cria um database efêmero por teste PostgreSQL; os três jobs da CI passaram.
- Na validação local de `605bbc5`: typecheck, build e ESLint direcionado passaram (3 avisos preexistentes em `router.tsx`); 5 E2E passaram, incluindo análise pela API e importação de renda variável/renda fixa com uma linha inválida. A CI passou em backend/PostgreSQL, frontend e Docker smoke. O lint global conhecido falha com 30 erros e 16 avisos em arquivos legados/gerados.

## Fases de implementação

### Fase 0 — revisar e estabilizar a implementação integrada

1. Revisar o diff completo e confirmar que os arquivos apagados estavam sem uso e que nenhuma fixture foi removida do Playwright por engano.
2. [x] Isolar cada teste PostgreSQL em um banco efêmero próprio; a suíte local passou sem skips e a CI de `aa31fc9` passou nos três jobs.
3. [x] Validar as rotas `/analise` e `/importar`, acesso autenticado, consultas e erros; 5 E2E passaram usando fixtures MSW explícitas.
4. [x] Executar typecheck, build e lint direcionado; o lint global mantém falhas legadas documentadas.
5. [x] Fazer commit da implementação frontend (`605bbc5`) e confirmar CI nos três jobs. Testes API/PostgreSQL cobrem ação e renda fixa, replay idempotente, rejeição e persistência após recriar o host. O E2E de navegador/API/PostgreSQL sem MSW também passou na CI de `9066682` (https://github.com/JonathanBenicio/invest-dashboard/actions/runs/36544411654); validar Brapi/Supabase reais continua pendente.
6. [x] Atualizar este plano e o catálogo de user stories com as evidências. Manter deploy em produção fora desta fase.

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

- No commit `605bbc5`: typecheck, build, lint direcionado (3 avisos existentes no router), 5 E2E com MSW e CI dos três jobs passaram. Na alteração atual: E2E sem MSW passou localmente e os quatro jobs, incluindo `csv-import-e2e`, passaram na CI `9066682` (https://github.com/JonathanBenicio/invest-dashboard/actions/runs/36544411654).
- Testes .NET locais em Release com PostgreSQL: 21 unitários e 28 integrações passaram, sem skips; cada teste PostgreSQL criou seu próprio database e foi removido ao final.
- CI de `a9ad5ac` passou. A CI de `df2bae7` ([execução 36538233870](https://github.com/JonathanBenicio/invest-dashboard/actions/runs/36538233870)) teve frontend/Docker smoke verdes e falhou no backend por migrations concorrentes. O corretivo `aa31fc9` isolou os bancos dos testes PostgreSQL; depois, `605bbc5` passou backend/PostgreSQL, frontend e Docker smoke com análise/importação incluídas.
- Após congelar cada fase, executar a validação correspondente e só então registrar seu aceite neste arquivo e em `docs/USER-STORIES.md`.

## Fora desta entrega

Fluxo de caixa, pagamentos de proventos e apuração fiscal completa. Esses itens exigem seus próprios contratos, critérios e validações; não devem ser substituídos por mocks na fase de posições.

## Fase futura solicitada: coleta do Tesouro Transparente

Registrada em 29/09/2026 a pedido do usuário. Esta automação ainda não foi implementada; só começa depois de finalizar e estabilizar as fases atuais.

### Objetivo

Executar uma coleta automática diária após o fechamento do mercado de títulos públicos, consultando o catálogo CKAN do Tesouro Transparente e trazendo CSVs publicados ou revisados. O ponto de partida indicado pelo usuário foi [busca CSV por Tesouro Prefixado](https://www.tesourotransparente.gov.br/ckan/dataset?res_format=CSV&tags=Tesouro+Prefixado). Para não restringir a coleta a Prefixado, a automação deverá descobrir os conjuntos e recursos pelo catálogo/API CKAN e pelas tags/metadados dos demais títulos.

### Conjuntos de dados e semântica

| Conjunto oficial | Conteúdo e periodicidade publicada | Uso planejado |
|---|---|---|
| [Taxas dos Títulos Ofertados pelo Tesouro Direto](https://www.tesourotransparente.gov.br/ckan/dataset/taxas-dos-titulos-ofertados-pelo-tesouro-direto) | Preços e taxas de compra/venda do mercado secundário, diariamente; os metadados indicam divulgação no primeiro dia útil após o fechamento. | Histórico de taxa e PU por título/vencimento, mantendo data de referência e data de coleta. |
| [Vendas do Tesouro Direto](https://www.tesourotransparente.gov.br/ckan/dataset/vendas-do-tesouro-direto) | Volume diário por título/vencimento, com defasagem publicada de dois dias úteis. | Estatística agregada de vendas do mercado, nunca uma compra de usuário. |
| [Estoque do Tesouro Direto](https://www.tesourotransparente.gov.br/ckan/dataset/estoque-do-tesouro-direto) | Volume total por título e mês; é calculado pela taxa de emissão e não representa valor de mercado. | Contexto agregado de estoque; não substituir o valor atual de uma posição individual. |
| [Resgates do Tesouro Direto](https://www.tesourotransparente.gov.br/ckan/dataset/resgates-do-tesouro-direto) | Base agregada com recompras antecipadas, vencimentos e cupons; a descrição fala em volume mensal e os recursos têm granularidades próprias. | Importar cada recurso separadamente, após confirmar sua unidade/frequência nos metadados. Não lançar como fluxo de caixa pessoal nesta fase. |
| [Operações do Tesouro Direto](https://www.tesourotransparente.gov.br/ckan/dataset/operacoes-do-tesouro-direto) | Operações do ponto de vista do Tesouro Nacional, classificadas em Venda, Compra, Retirada e Depósito; há recursos anuais de centenas de MiB e uma base completa muito maior. | Usar para análise agregada e reconciliação de dados públicos. Nunca tratar como extrato ou operação individual do investidor. |

O Tesouro publica URLs `package_show` CKAN nas páginas dos conjuntos. A execução deverá consultar o catálogo e escolher o recurso CSV ativo por `resource_id`/revisão, em vez de fixar links de download ou assumir que o filtro por `Tesouro Prefixado` cobre todos os papéis. As páginas identificam licença ODbL; preservar atribuição e registrar a fonte em cada carga. A [página oficial de dados abertos do Tesouro Direto](https://www.tesourotransparente.gov.br/temas/divida-publica-federal/tesouro-direto) descreve a cadência e os recursos; os [metadados de taxas](https://www.tesourotransparente.gov.br/ckan/dataset/taxas-dos-titulos-ofertados-pelo-tesouro-direto/resource/1a8eb2e3-4902-4a38-a1eb-6410f23d90de) especificam a divulgação após o fechamento. [Operações](https://www.tesourotransparente.gov.br/ckan/dataset/operacoes-do-tesouro-direto) e [estoque](https://www.tesourotransparente.gov.br/ckan/dataset/estoque-do-tesouro-direto) confirmam, respectivamente, a perspectiva do emissor e que estoque não é valorado a mercado.

### Desenho proposto

1. Criar um serviço backend independente da Brapi para consultar o catálogo CKAN, resolver `package_id` e recursos CSV atuais e registrar metadados da execução.
2. Agendar diariamente em `America/Sao_Paulo` após o horário oficial de fechamento, com uma margem configurável. A chegada dos dados não é simultânea ao fechamento: taxas publicam no próximo dia útil, vendas podem vir com dois dias úteis de atraso e estoque é mensal. Consultar cada recurso segundo sua cadência/defasagem e repetir com backoff enquanto a data esperada ainda não foi publicada.
3. Persistir checkpoint por conjunto, recurso e revisão/hash (`ETag`/`Last-Modified` quando disponíveis), mais a data de referência do dado. Duas execuções do mesmo recurso não podem duplicar linhas.
4. Fazer download e parsing em streaming/lotes por causa do tamanho das bases anuais de operações. Validar encoding, delimitador, decimal, cabeçalho e versões usando o PDF de metadados correspondente antes de processar os dados completos.
5. Separar fatos de taxa/PU, venda agregada, estoque agregado, recompra/vencimento/cupom e operação do emissor. Não escrever automaticamente esses totais em `Transacao`, `PosicaoInvestimento` ou saldo do usuário.
6. Disponibilizar os dados e metadados ao frontend, incluindo data de referência, horário da última coleta, fonte, estado de atualização e erros parciais. Em atraso, conservar o último dado carregado com aviso; não fabricar valores.
7. Criar uma área informativa do Tesouro Direto no frontend com filtros por título, vencimento e período. Usar gráficos de histórico de taxas e preços unitários (PU), além de tabelas de cotações e séries carregadas. Apresentar vendas, estoque, resgates e operações do emissor em visualizações agregadas próprias, sempre identificando unidade, frequência, período, fonte e data de referência.
8. Quando o usuário tiver uma posição individual compatível cadastrada, mostrar seus dados de quantidade, custo e valor estimado ao lado da série oficial correspondente e permitir comparação temporal da posição com taxas/PU observados. Exibir separadamente os agregados públicos como contexto de mercado; não os somar à posição, atribuí-los ao usuário nem compará-los como se fossem desempenho individual. O estoque oficial não é valor de mercado. Se o título/vencimento ou a unidade não puderem ser conciliados com segurança, mostrar o dado público sem comparação individual e explicar a limitação.
9. Não coletar a base de investidores. Ela contém dados de perfil/cadastro e não é necessária para a função solicitada.

### Aceite da fase futura

- O scheduler respeita calendário/horário local e a cadência e defasagem específicas de cada recurso.
- A mesma revisão pode ser processada mais de uma vez sem duplicar fatos; recursos grandes são processados sem carregar o arquivo inteiro em memória.
- Validação automatizada usa amostras oficiais versionadas de cada CSV e detecta mudança de esquema, datas ausentes, duplicatas e totais incompatíveis.
- A interface oferece gráficos e tabelas filtráveis para taxa/PU e para cada conjunto agregado, indicando título, vencimento, unidade, periodicidade, fonte e data de referência.
- Para uma posição cadastrada, a comparação individual usa apenas série oficial com instrumento, vencimento, unidade e datas compatíveis; posição do usuário e estatísticas agregadas ficam visualmente e semanticamente separadas.
- Nenhum número agregado é confundido com posição, compra ou fluxo de caixa individual; estoque não é apresentado como valor de mercado.
- A coleta falha sem sobrescrever dados válidos e publica logs/alertas com recurso e data que falharam.
