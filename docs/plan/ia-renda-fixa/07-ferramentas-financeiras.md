# AS-02 · Ferramentas de carteira e análises

- Estado: planejado
- Épico: [mapa e dependências](epico.md)
- Issue: [#79](https://github.com/JonathanBenicio/invest-dashboard/issues/79)
- Page: [abrir plano](https://chatgpt.com/space/page_e17d5aede76c8191adc2c503f2deee1b)
- Baseline: 27/11/2026 · `codex/tesouro-previa-e-cotacoes` · `d683cfe` · `.codex-attachments/` preexistente.
- Dependências: AS-01, RF-02 para projeções avançadas
- Agentes: invest-backend, invest-api, invest-test

## Objetivo e escopo

Ferramentas de carteira e análises conforme o [épico](epico.md). O recorte e o resultado verificável estão definidos nas etapas e critérios abaixo.

## Etapas e verificações

- Expor obter_resumo_carteira, listar_posicoes e obter_detalhes_posicao em lista permitida. Injetar identidade e escopo no servidor. Filtrar explicitamente carteiras próprias no MVP e validar vínculo da posição.
- Reusar serviços atuais para distribuição, comparação, movimentos, proventos e projeções suportadas. Retornar BRL, data, fonte, semântica e motivo da ausência. Manter cálculo C# e resultados tipados para tabelas/gráficos.
- Limitar tamanho do contexto, chamadas por turno e paginação. Basear ajuda de uso em documentação selecionada.

## Critérios de aceite

- [ ] Respostas coincidem com os serviços financeiros.
- [ ] IDs de outra carteira ou posição são recusados.
- [ ] Líquido sem insumo é indisponível, não estimado pelo modelo.

## Validação, riscos e entrega

Evidências de implementação pendentes. Aplicar os controles e os testes descritos nesta página e em [QA-01](14-homologacao.md). Registrar resultados, dependências por capacidade e links de entrega na issue e na Page.
