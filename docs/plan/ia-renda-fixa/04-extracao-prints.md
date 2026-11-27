# TD-01 · Extração e API de prévias

- Estado: planejado
- Épico: [mapa e dependências](epico.md)
- Issue: [#76](https://github.com/JonathanBenicio/invest-dashboard/issues/76)
- Page: [abrir plano](https://chatgpt.com/space/page_03ecc0af68688191b229bff8b7b9d6bc)
- Baseline: 27/11/2026 · `codex/tesouro-previa-e-cotacoes` · `d683cfe` · `.codex-attachments/` preexistente.
- Dependências: IA-01, IA-02 para nuvem
- Agentes: invest-backend, invest-api, invest-database, invest-test

## Objetivo e escopo

Extração e API de prévias conforme o [épico](epico.md). O recorte e o resultado verificável estão definidos nas etapas e critérios abaixo.

## Etapas e verificações

- Criar POST /api/v1/portfolios/{carteiraId}/tesouro-direto/previas-imagem e GET /{previaId}. Validar sessão e gestão antes da leitura. PNG, JPEG, WebP e limite inicial de 10 MiB, com limites de dimensões, pixels e duração testados.
- Extrair título, vencimento, quantidade, saldo e natureza, valor aplicado, data do saldo e rentabilidade com período, valor e percentual. Preservar ausências, conteúdo transcrito e campos normalizados. “Saldo” genérico fica indeterminado.
- Persistir carteira, autor, ID, itens, hash, provedor, versão, estado e expiração. Descartar bytes após leitura. Repetição do identificador na carteira do autor reaproveita o resultado.

## Critérios de aceite

- [ ] Fixture sintética preserva as datas distintas e natureza indeterminada.
- [ ] Testar múltiplos títulos, arquivo inválido, acesso, fallback e data ausente.
- [ ] Criar prévia causa zero alterações em posição, avaliação ou movimento.

## Validação, riscos e entrega

Evidências de implementação pendentes. Aplicar os controles e os testes descritos nesta página e em [QA-01](14-homologacao.md). Registrar resultados, dependências por capacidade e links de entrega na issue e na Page.
