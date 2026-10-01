# Plano: analisar e importar CSVs de corretoras

Este plano prepara a importação de extratos reais sem presumir layouts antes de receber amostras. O importador CSV genérico atual continua disponível e grava transações pela API; os adaptadores abaixo devem ser adicionados somente após verificar arquivos representativos.

## Resultado esperado

Um arquivo exportado por uma corretora passa por identificação de layout, prévia e validações; somente linhas sem ambiguidade são enviadas ao ledger existente. Repetir a importação não duplica operações. Cada linha mostra sucesso, duplicidade ou erro com motivo legível.

## Insumos necessários

Para cada corretora e versão do extrato, obter 2 a 3 CSVs anonimizados cobrindo compra, venda, taxas, mais de uma data e pelo menos um caso de parcial/cancelamento se o formato tiver esses eventos. Antes de compartilhar, remover CPF, nome, e-mail, número de conta, agência, endereço, identificadores de ordem privados, saldo e qualquer informação não necessária; substituir por valores sintéticos, preservando cabeçalhos, delimitadores, convenção decimal, aspas e estrutura. Enviar um arquivo de cabeçalho mais linhas representativas e registrar nome da corretora, produto/tipo de extrato, intervalo de datas e versão/data de exportação. Não colar senha, token ou chave no CSV.

## Etapas

1. **Inventário e classificação:** registrar corretora, tipo de exportação, versão/layout, extensão/encoding, delimitador, locale, datas, timezone e se linhas representam ordens, execuções, liquidações ou posição consolidada. Não misturar conceitos.
2. **Mapeamento:** descrever cabeçalho de origem → campos internos, regras de sinal/unidade, códigos de evento, taxas, identificador estável da operação e campos que não podem ser inferidos. Preservar o CSV original no ambiente local do usuário; o sistema persiste somente dados normalizados e metadados mínimos da origem aprovados.
3. **Análise comparativa:** construir fixtures sintéticas a partir das amostras anonimizadas; comparar total de compras/vendas, taxas, quantidades e variação de posição com o extrato. Registrar ambiguidades e decisões por layout em uma tabela de compatibilidade.
4. **Pré-validação sem gravação:** reconhecer layout por assinatura de cabeçalho, validar linha inteira, exibir origem, data, classe/ticker, lado, quantidade, preço, taxa, valor calculado e avisos. Detectar separador decimal/milhar, timezone, lotes fracionários, linhas de subtotal, estornos e cancelamentos; nunca converter evento desconhecido em compra/venda.
5. **Confirmação e importação:** usuário seleciona carteira, revisa prévia e confirma; a API importa em transações idempotentes por corretora/layout/id da execução, sem sucesso parcial silencioso. Linha ambígua ou inválida fica rejeitada sem mutação.
6. **Reconciliação e auditoria:** mostrar resumo importado/ignorado/rejeitado e diferenças por quantidade/valor/taxa; permitir baixar relatório de validação. Reimportar mesmo arquivo resulta em duplicatas reconhecidas e ledger inalterado.
7. **Validação:** testes de parsing por fixture, cultura pt-BR/en-US, BOM/UTF-8, delimitador, aspas, datas, compra/venda, taxas, múltiplas corretoras, repetição, falha parcial, concorrência e ledger pós-importação em PostgreSQL. E2E só com fixtures artificiais, nunca credenciais ou extrato identificável.

## Fora de escopo inicial

Conexões diretas/API de corretoras, envio de ordens, armazenar arquivos pessoais, interpretação fiscal automática, proventos/fluxo de caixa, conciliação bancária, XLS/XLSX, eventos corporativos não explicitados e inferência de day trade a partir de data sem horário/modalidade confiáveis.

## Critérios de aceite

- Cada layout tem amostra anonimizada versionada e documentação do mapeamento.
- Prévia apresenta totais e erros antes de gravar; cabeçalho desconhecido é rejeitado.
- Importação idempotente, linhas ambíguas não alteram o ledger e reimportação não duplica.
- Quantidade, custo, taxas e posição reconciliam com amostras após regras explicitamente aprovadas.
- Testes PostgreSQL e E2E verificam persistência, repetição e relatório de rejeições.

## Dependência atual

Nenhuma amostra de corretora foi anexada. A primeira ação quando os arquivos estiverem disponíveis é analisá-los e preencher a tabela de layout; não será criado parser específico com base em suposição.
