---
name: invest-msbuild
description: Review .csproj, .slnx, .props, and .targets files or diagnose .NET restore/build/test failures and measured slow builds in the Invest Dashboard solution.
---

# Diagnóstico MSBuild do Invest Dashboard

Use quando pedirem revisão de `.csproj`/`.slnx`/`.props`/`.targets`, ou houver falha de build/restore, comportamento de MSBuild ou build lento com evidência. A solução é `src/InvestDashboard.slnx` e os seis projetos são SDK-style em .NET 10. Consulte `src/AGENTS.md` e `.github/workflows/ci.yml`; o CI executa restore seguido por `dotnet test ... --no-restore`.

## Falhas de build/restore

1. Reproduza o comando e projeto mais estreitos possíveis; capture erro completo, código de saída, SDK e configuração. Separe erro de compilador, restore, assets desatualizados e falha de teste.
2. Inspecione `.csproj`, `global.json` se existir, referências de projeto e configurações de diretório que realmente participam do build. Não presuma Directory.Build.props ou central package management sem encontrá-los.
3. Corrija a causa, evitando limpeza ampla que apague evidência. Reexecute primeiro o projeto afetado e depois a validação adequada da solução.

## Build lento

Meça um baseline repetível antes de recomendar paralelismo, cache ou alterações estruturais. Se logs forem necessários, peça um binlog somente quando isso for útil e autorizado: binlogs podem conter caminhos, propriedades e valores de configuração sensíveis. Revise e sanitize antes de compartilhar; não gere ou envie binlogs automaticamente.

Não modernize arquivos de projeto SDK-style, centralize pacotes ou introduza MCP como efeito colateral de um diagnóstico. Relate duração e máquina/configuração; comparações sem condições equivalentes são inconclusivas.

Para revisar um PR focado em `.csproj`, `.slnx`, `.props` ou `.targets`, limite a revisão aos arquivos alterados e dependências diretamente relacionadas. Procure semântica de restore/referências, ordem de targets, build incremental e inclusão/cópia de arquivos; marque hipótese de desempenho como hipótese e não faça build do PR por padrão apenas para revisão estática.

## Inspiração e licença

Esta skill é uma adaptação original para o monorepo, baseada em conceitos do plugin [`dotnet-msbuild`](https://github.com/dotnet/skills/tree/96c3d68dad2855e3ab5178aa1a262789d86a1466/plugins/dotnet-msbuild), licenciado em MIT no repositório upstream. Não copia o seu agente, ferramentas MCP ou conteúdo de referência.
