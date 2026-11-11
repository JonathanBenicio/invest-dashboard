# Regras de commit

- Escreva sempre o assunto e, quando houver, o corpo da mensagem de commit em português brasileiro (pt-BR). Preserve identificadores técnicos, nomes próprios, tipos convencionais e trailers do Git quando aplicáveis.
- Use assunto curto no formato `tipo(escopo): resultado`, por exemplo `docs(workflow): registra processo de contribuição`. Tipos usuais: `feat`, `fix`, `docs`, `test`, `refactor`, `chore`, `perf`, `build`, `ci`.
- Inclua Issue para trabalho rastreável. Use trailer `Refs #123` quando a entrega for parcial, o trabalho continuar ou critérios permanecerem abertos. Use `Closes #123` somente quando este conjunto de commits cumpre todos os critérios verificáveis da Issue; o PR também deve trazer evidências.
- Revise `git status` e o diff, selecione arquivos/caminhos explicitamente e confirme conteúdo staged com `git diff --cached`. Nunca use `git add .`/`git add -A` como atalho. Preserve alterações alheias e não remova/restaure arquivos de terceiros.
- Não inclua segredos, tokens, dados pessoais/financeiros ou logs brutos. Documente evidência no plano/relatório/PR, sem depender de notas de commit obrigatórias.
- Não crie commits vazios. O trailer não substitui links da Issue para ADR/Story/Plano/PR.
