# Homologação de autenticação Supabase

**Agendamento:** pós-merge do PR #44, por decisão do usuário em 05/10/2026. Não bloqueia o merge; validar antes do deploy. Os passos abaixo permanecem pendentes de execução.

## Preparar contas e dados de teste

No projeto **Invest Dashboard**, abra **Authentication → Users → Add user**. Prepare duas contas dedicadas e confirmadas, cada uma com um grupo, carteira e posição de teste próprios, sem compartilhar membros entre os grupos. Use valores sintéticos. Para o cenário de conta não confirmada, mantenha uma terceira conta separada sem clicar no link de confirmação. Evite usar a conta pessoal que administra o projeto.

O teste não precisa de chave `secret`, `service_role`, JWT secret ou chave publishable extra. A URL e a publishable key que já existem no `.env` local do Compose são usadas pela API. A secret key nunca deve ser colocada no browser, no frontend nem enviada no chat.

### Conferir configuração de redirect

A leitura somente de `GET /auth/v1/settings` confirmou que o provider de e-mail está habilitado, `mailer_autoconfirm` está desativado e o cadastro está aberto. Esse endpoint público não retorna `site_url` nem a allowlist de redirect URLs. A URL de convite está configurada localmente, mas sua presença na allowlist não foi verificada: confira **Authentication → URL Configuration** no Dashboard e garanta que o destino configurado para convite seja permitido. A documentação do Supabase informa que um `redirectTo` fora da allowlist é ignorado e o convite usa o Site URL como destino ([Redirect URLs](https://supabase.com/docs/guides/auth/redirect-urls), [Users](https://supabase.com/docs/guides/auth/users)).

## Colocar os dados localmente

Copie `frontend/.env.e2e.example` para `frontend/.env.e2e.local` e preencha credenciais das duas contas confirmadas, credenciais da conta não confirmada e os IDs dos recursos separados:

```dotenv
E2E_API_URL=http://127.0.0.1:5000
E2E_SUPABASE_EMAIL=conta-confirmada-a@exemplo.com
E2E_SUPABASE_PASSWORD="senha-da-conta-confirmada-a"
E2E_SUPABASE_SECONDARY_EMAIL=conta-confirmada-b@exemplo.com
E2E_SUPABASE_SECONDARY_PASSWORD="senha-da-conta-confirmada-b"
E2E_SUPABASE_UNCONFIRMED_EMAIL=conta-nao-confirmada@exemplo.com
E2E_SUPABASE_UNCONFIRMED_PASSWORD="senha-da-conta-nao-confirmada"
E2E_SUPABASE_PRIMARY_GROUP_ID=uuid-do-grupo-a
E2E_SUPABASE_PRIMARY_PORTFOLIO_ID=uuid-da-carteira-a
E2E_SUPABASE_PRIMARY_POSITION_ID=uuid-da-posicao-a
E2E_SUPABASE_SECONDARY_GROUP_ID=uuid-do-grupo-b
E2E_SUPABASE_SECONDARY_PORTFOLIO_ID=uuid-da-carteira-b
E2E_SUPABASE_SECONDARY_POSITION_ID=uuid-da-posicao-b
```

`frontend/.env.e2e.local` é ignorado pelo Git. Não prefixe essas variáveis com `VITE_`: são segredos para o processo do teste, não dados do app/browser. Não envie o conteúdo desse arquivo em chat, screenshots, logs ou commits.

## Executar e o que será validado

Com o Compose local ativo, rode `bun run test:api --grep "Supabase Auth live smoke"` a partir da pasta `frontend`. Os cenários cobrem login válido/inválido e não confirmado, claims e `/me`, rotação/replay/logout, atributos do cookie e isolamento de leitura/mutação entre grupos, carteiras e posições. As falhas de indisponibilidade do provedor são cobertas por testes determinísticos de API com um provider que falha. A suíte não cria contas nem recursos: os IDs apontam para dados descartáveis previamente preparados. Sem as credenciais/IDs correspondentes, o cenário é ignorado sem tentar login.

Os testes automatizados verificam a diretiva `Secure` do cookie emitido pelo Compose (ambiente Production), mas não simulam navegação do navegador sob HTTPS. O teste multiusuário tenta mutações apenas contra recursos do outro usuário e espera 403/404; ele não altera nem apaga os recursos configurados.

O endpoint `/api/v1/auth/login` valida a senha com Supabase, descarta/revoga o access token upstream e então emite uma sessão JWT/cookie própria da API; este teste não valida consumo direto de JWT Supabase via JWKS. Refresh e logout são operações da sessão local da API e não renovam nem chamam logout da sessão upstream Supabase.
