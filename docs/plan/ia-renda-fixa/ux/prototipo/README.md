# Protótipo do assistente híbrido

Demonstração isolada de UX-01, com dados fictícios e estado local de sessão. A consulta rápida e a conversa completa compartilham mensagens. A prévia permite conferir os campos e voltar ao ponto de origem. A seleção de imagem fica na consulta rápida. O protótipo simula uma resposta e sua interrupção.

## Executar localmente

No diretório do protótipo, execute `npm ci` e `npm run dev -- --host 127.0.0.1`. Para validar a distribuição, execute `npm run build` e `npm run test:sites`.

## Evidências

- [Carteira e consulta rápida em desktop](artifacts/hybrid-1366x768-final.png)
- [Carteira em celular](artifacts/ux01-360-carteira.png)
- [Conversa em celular](artifacts/ux01-360-conversa.png)
- [Prévia em celular](artifacts/ux01-360-revisao.png)
- [Auditoria, fluxos e matriz de estados](../auditoria.md)

O protótipo tem um provedor demonstrativo e uma prévia sintética fixa. A matriz descreve os estados a implementar nos subplanos, incluindo fallback entre provedores. A revisão demonstrativa registra apenas uma marcação local.
