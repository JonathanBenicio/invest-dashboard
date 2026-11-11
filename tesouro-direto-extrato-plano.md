# Plano — importação de extrato do Tesouro Direto

**Iniciativa GitHub:** https://github.com/JonathanBenicio/invest-dashboard/issues/53

**Situação:** planejamento; duas amostras locais são planilhas XLSX de extrato analítico, não arquivos CSV. Têm uma aba cada e cabeçalhos de dados do investimento, quantidade, aplicação, valor investido e situação em eventual resgate. Os arquivos contêm dados pessoais/financeiros: não versionar nem publicar. A interpretação detalhada e a origem/exportação exata precisam de confirmação.

**Objetivo:** importar posições e valores atuais do extrato para uma carteira selecionada, com prévia e conciliação, preservando distinção entre posição, avaliação e operação. O importador existente aceita somente CSV de operações e rejeita XLSX; não enviar automaticamente posições como compras/vendas.

- [ ] Inventariar o layout real das duas amostras localmente: seções, linhas, cabeçalhos, datas de referência, tipos e fórmulas; produzir fixture sintética sem dados pessoais. Determinar se há movimentações efetivas ou apenas posições e lotes.
- [ ] Prioridade confirmada pelo usuário: posições e valores atuais; histórico e ledger ficam fora da entrega inicial. Decidir reconciliação de registros existentes, valores brutos/líquidos, taxas, vencimento e duplicatas; não inferir operações ausentes. ADR proposta para identidade, atualização, persistência e conflitos; US-IMPORT-001 como referência parcial.
- [ ] Especificar suporte XLSX direto e/ou CSV convertido, identificando versões, cabeçalhos, locale, separadores e rejeição de arquivos desconhecidos. Confirmar formato-alvo antes de implementar.
- [ ] Implementar leitura segura e prévia por carteira: título, lote, quantidade, data de aplicação, valores de origem, data de referência, avisos, erros e divergências; nenhuma gravação antes da confirmação. Não supor que valor de resgate é custo ou cotação atual.
- [ ] Implementar persistência somente após decisão de domínio; preservar acesso por grupo/usuário, registrar proveniência mínima e garantir reimportação idempotente, detecção de alterações e resolução explícita de conflitos, sem sobrescrita silenciosa.
- [ ] Validar com fixtures sintéticas e PostgreSQL: múltiplos lotes, decimal pt-BR, datas, subtotal, vazios, dados inválidos, duplicidade, conciliação e permissões; E2E cobre prévia, confirmação e reimportação. Comparar totais localmente com extratos sem publicar valores.

**Limites:** extrato privado do usuário não é a base pública Tesouro Transparente. Sem inventar lançamentos, saldos ou regras fiscais; não armazenar o arquivo original por padrão. Documentação e decisões estão pendentes; histórico e importação de operações não fazem parte da prioridade inicial.
