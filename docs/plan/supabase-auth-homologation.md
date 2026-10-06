# Homologação de autenticação Supabase

**Agendamento:** pós-merge do PR #44, por decisão do usuário em 05/10/2026. Não bloqueia o merge; validar antes do deploy. Os passos abaixo permanecem pendentes de execução.

## Criar/selecionar uma conta de teste

No projeto **Invest Dashboard**, abra **Authentication → Users → Add user**. O painel atual oferece **Create new user** e **Send invitation**. Use uma conta dedicada de teste, com e-mail acessível e senha conhecida. Se convidar, complete o link de confirmação recebido por e-mail antes de testar: a configuração observada deste projeto exige confirmação de e-mail para entrar. Evite usar a conta pessoal que administra o projeto.

O teste não precisa de chave `secret`, `service_role`, JWT secret ou chave publishable extra. A URL e a publishable key que já existem no `.env` local do Compose são usadas pela API. A secret key nunca deve ser colocada no browser, no frontend nem enviada no chat.

## Colocar os dados localmente

Copie `frontend/.env.e2e.example` para `frontend/.env.e2e.local` e preencha somente:

```dotenv
E2E_API_URL=http://127.0.0.1:5000
E2E_SUPABASE_EMAIL=conta-de-teste@exemplo.com
E2E_SUPABASE_PASSWORD="senha-da-conta-de-teste"
```

`frontend/.env.e2e.local` é ignorado pelo Git. Não prefixe essas variáveis com `VITE_`: são segredos para o processo do teste, não dados do app/browser. Não envie o conteúdo desse arquivo em chat, screenshots, logs ou commits.

## Executar e o que será validado

Com o Compose local ativo, rode `bun run test:api --grep "Supabase Auth live smoke"` a partir da pasta `frontend`. O teste faz login na API local, verifica e-mail/claims, confirma rotação do refresh cookie, acessa `/me`, faz logout e verifica que o refresh deixa de funcionar. Tokens e senha não são escritos na saída. Sem as duas variáveis de conta, o teste aparece como ignorado, sem tentar login.

O endpoint `/api/v1/auth/login` valida a senha com Supabase, descarta/revoga o access token upstream e então emite uma sessão JWT/cookie própria da API; este teste não valida consumo direto de JWT Supabase via JWKS. Refresh e logout são operações da sessão local da API e não renovam nem chamam logout da sessão upstream Supabase.
