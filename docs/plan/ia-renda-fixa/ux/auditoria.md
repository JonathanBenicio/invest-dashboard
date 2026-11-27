# Auditoria e handoff de UX

- Subplano: [UX-01](../01-descoberta-ux.md)
- Estado: captura e ideação em andamento. Seleção de direção pendente.
- Referência: dashboard InvestPro, navegação lateral, tema escuro, componentes React/shadcn e tokens atuais.

## Tarefas para comparar nas três propostas

Selecionar uma carteira sintética, consultar saldo observado de Tesouro, conferir qual provedor atendeu e abrir uma prévia de print para revisão na importação. Na revisão, separar data do saldo, período de rentabilidade e natureza do valor. Diferenciar saldo observado, marcação a mercado e projeção com rótulo, fonte e data.

## Matriz de estados

| Fluxo | Estados e comportamento esperado |
|---|---|
| Configuração | Chave ausente ou configurada sem revelar valor, teste em andamento, sucesso, modelo incompatível, provedor fora, conflito na gravação. |
| Chat | Escolha de carteira ou grupo, vazio, envio, streaming, interrupção, erro parcial, histórico e escopo revogado. Preservar foco e anunciar estado sem reler cada token. |
| Provedor | Local ou nuvem elegível, fallback com identificação do provedor efetivo, nenhum disponível, cota esgotada. |
| Print | Anexo escolhido, processamento, prévia editável, leitura incompleta, saldo indefinido, conflito ou prévia expirada. Registrar saldo bruto somente com revisão e confirmação. |
| Renda fixa | Dados suficientes, premissas ausentes, alerta contextual explicado, valores observados e projetados com datas distintas. |

## Handoff após escolha

Detalhar desktop e celular, teclado, foco, semântica da tabela e estados sem cor como único indicador. Conferir reflow a 320 px e navegação a 767/768 px quando afetada. Entregar wireframes e protótipo no subplano UX-01 e referenciar IA-02, TD-02, AS-03, AS-04, AS-05, RF-01 e RF-02.
