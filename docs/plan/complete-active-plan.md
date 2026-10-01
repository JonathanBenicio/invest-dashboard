# Concluir o plano ativo de posições

Objetivo: fechar os gaps de acompanhamento de posições sem acrescentar fluxo de caixa, que está no [backlog futuro de baixa prioridade](future-low-priority.md).

1. [x] Marcar renda fixa vencida em `America/Sao_Paulo` sem alterar quantidade, valor, saldo ou ledger. Testes de domínio, integração e persistência em PostgreSQL passaram.
2. [x*] Implementar estimativa fiscal mensal versionada para ações comuns, day trade e FII identificados. `*` Revisão profissional continua necessária antes de release; a aplicação sinaliza exclusões/dados incompletos e não é orientação fiscal.
3. [x] Implementar CDI diário BCB SGS 12, composição, cache e comparação normalizada com carteira em datas comuns. O usuário moveu Ibovespa B3 para funcionalidade futura.
4. [~] Brapi pública validada pelo cliente de produção; limites/token contratados não homologados. Supabase sem conta de homol.; [passos locais de preparação](supabase-auth-homologation.md) e teste opt-in estão prontos. Testes fakes aprovados e Compose/PostgreSQL validados.
5. [~] Plano de análise/importação de CSVs pronto em [broker-csv-import-plan.md](broker-csv-import-plan.md). Aguardar amostras reais anonimizadas para validar layouts, totais e deduplicação; importação genérica API/Postgres está coberta.
6. [~] Estado atual: build, 47 unitários (2 testes live ignorados), 66 integrações PostgreSQL, 14 E2E MSW e 11 E2E UI/API/PostgreSQL passaram; lint tem zero erros e dez avisos existentes. As falhas locais da Brapi têm cobertura determinística de HTTP, timeout, rede e resposta inválida. Deploy continua dependente da homologação real de Supabase/Brapi, revisão fiscal e licença B3; Ibovespa permanece futuro.

Decisões do usuário: vencimento apenas marca a posição e preserva o valor; IR será apresentado como estimativa com fonte/regras versionadas; CDI vem do Banco Central. A série Ibovespa B3 foi movida para funcionalidade futura. A coleta/visualização do Tesouro Transparente permanece no plano futuro específico.
