# Plano para fechar os gaps das telas que usavam mocks

## Escopo decidido

- Titular é um perfil separado da conta de login, criado dentro do grupo, com nome e parentesco opcional. Parentesco é informativo e não concede acesso. Um perfil pode ser vinculado a um membro ativo; carteira particular fica acessível ao titular vinculado e aos Admins. Sem vínculo, só Admin acessa. O autor de cada movimentação permanece separado do titular.
- Instituições usam IDs estáveis para bancos, corretoras e DTVMs conhecidos. Cada grupo pode cadastrar instituições próprias na opção “Outra”; carteiras guardam o ID da instituição, separado do emissor do ativo.
- Taxas vencidas continuam visíveis como observadas, mas bloqueiam o número projetado e informam qual premissa deve ser atualizada. Limites: 5 dias úteis financeiros para taxa diária, contando feriados nacionais e dias não úteis do mercado (Carnaval, Sexta-feira da Paixão, Corpus Christi e Finados), conforme a [Resolução CMN 2.932](https://normativos.bcb.gov.br/Lists/Normativos/Attachments/46961/Res_2932_v4_L.pdf), a [Lei 14.759/2023](https://www.planalto.gov.br/ccivil_03/_ato2023-2026/2023/lei/l14759.htm) e o calendário da [B3](https://www.b3.com.br/pt_br/noticias/calendario-de-negociacao-da-b3-confira-o-funcionamento-da-bolsa-em-2026.htm); 45 dias corridos para mensal; 400 dias corridos para anual. Taxa pontual não entra em projeção.

## Implementação

- [x] Carteiras: grupo, titular, instituição financeira, visibilidade Particular/Pública, quatro indicadores e totais calculados pelo servidor sem truncamento de paginação.
- [x] Titulares: perfis por grupo, vínculo opcional com membro ativo, parentesco informativo e autor de movimentação preservado separadamente.
- [x] Instituições: catálogo global de IDs estáveis, cadastro “Outra” por grupo e associação da carteira por ID.
- [x] Renda fixa: liquidez opcional, valor observado do último extrato, projeção bruta até vencimento na listagem e nos detalhes; projeção consolidada abrange todas as posições acessíveis e nunca cria saldo em caixa. Premissas ausentes ou vencidas não geram número estimado.
- [x] Usuários: grupos e convites, papéis Admin/Investidor/Consulta por grupo e autorização validada no servidor.
- [x] Taxas: escopo por grupo, histórico com unidade e periodicidade, origem, data de referência e responsável; modelos vazios SELIC, CDI, IPCA, USD/BRL e IR-PF.
- [x] Vendas de ações/FIIs exigem modalidade fiscal na interface e na API; criação de taxa retorna URL com o grupo.

## Validação

- [x] Suíte .NET Release executada contra PostgreSQL 15 em Docker: 47 testes unitários e 66 de integração aprovados; 2 testes live que dependem de serviços externos ficaram ignorados. Os testes de contrato Brapi cobrem 401/403/429, timeout, falha de rede, JSON inválido, cotação sem horário, ticker ausente e histórico vazio.
- [x] Revisão de membros: aceitar convite não pode rebaixar o último Admin ativo; papéis numéricos indefinidos são rejeitados. Testes cobrem Investidor/Consulta, 999/0/-1, rebaixamento permitido com outro Admin e preservação do membro/convite em PostgreSQL.
- [x] E2E Playwright: 14/14 cenários MSW e 11/11 fluxos reais UI/API/PostgreSQL aprovados, sem flakiness; o único cenário ignorado é a homologação live de Supabase, que requer credenciais de teste. A cobertura real inclui grupo/titular/instituição/visibilidade, venda fiscal, projeção sem taxa, histórico de taxa, importação CSV e avaliação manual de renda fixa.
- [x] Typecheck (`bun x tsc -p tsconfig.app.json --noEmit`) e build de produção (`bun run build -- --outDir /tmp/invest-dashboard-final-build`) aprovados. ESLint: 0 erros e 10 avisos existentes de Fast Refresh; o build também informa Browserslist desatualizado e bundle principal acima de 500 KB.
- [x] Migrações atuais foram exercitadas pela suíte de integração PostgreSQL, incluindo persistência de perfis/instituições e metadados do histórico.
## Critérios de conclusão

- Nenhum fixture é apresentado como valor real.
- Totais e projeções indicam o conjunto considerado e não dependem da página atual.
- Observações e estimativas permanecem separadas e estimativas indisponíveis explicam a premissa faltante ou vencida.
- O servidor aplica autorização por grupo e visibilidade em cada leitura e alteração.
