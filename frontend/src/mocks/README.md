# MSW (Mock Service Worker) - Configuração Multi-Plataforma

Este diretório contém a configuração do MSW para simular uma API durante o desenvolvimento, funcionando em múltiplas plataformas.

## 📱 Plataformas Suportadas

| Plataforma | URL Base | Status |
|------------|----------|--------|
| **Web Development** | `http://localhost:5000` | ✅ |
| **GitHub Pages** | `https://jonathanbenicio.github.io/invest-dashboard/` | ✅ |
| **Android (Capacitor)** | `https://invest-dashboard/` | ✅ |

## 🏗️ Arquitetura

```
src/mocks/
├── browser.ts      # Configuração do worker para navegador
├── handlers.ts     # Handlers de requisições HTTP
├── data.ts         # Dados mock (usuários, portfolios, investimentos)
└── README.md       # Este arquivo
```

## ⚙️ Como Funciona

### 1. Ativação do MSW

O MSW é ativado quando `VITE_USE_MSW=true` no ambiente:

```typescript
// src/main.tsx
const shouldUseMSW = useByFlag
```

### 2. Configuração de URLs

O segredo para funcionar em múltiplas plataformas está na configuração de URLs:

```typescript
// src/api/env.ts
BASE_URL: (import.meta.env.VITE_USE_MSW === 'true')
  ? ''  // ← Vazio = requisições relativas
  : (import.meta.env.VITE_API_URL || 'http://localhost:5000')
```

```typescript
// src/mocks/handlers.ts
const BASE_URL = API_CONFIG.VERSION  // '/api/v1'
```

**Por que funciona?**
- Requisições relativas (`/api/v1/auth/login`) são feitas para a origem atual
- O MSW casa paths relativos automaticamente em qualquer domínio
- Não importa se é `localhost`, `github.io` ou `invest-dashboard` (Capacitor)

### 3. Cliente HTTP

O cliente trata URLs relativas corretamente:

```typescript
// src/api/client.ts
const url = apiPath.startsWith('http') 
  ? new URL(apiPath) 
  : new URL(apiPath, window.location.origin)
```

### Contrato JSON

Os handlers refletem o contrato atual da API em `/api/v1`: propriedades e filtros próprios usam português em `camelCase`; as rotas de recursos permanecem em inglês. Respostas seguem `dados/sucesso/mensagem`, e listas paginadas usam `paginacao` com `pagina`, `itensPorPagina`, `totalItens` e metadados de navegação. A referência completa está em [API-CONTRACTS-PT-BR.md](../../../docs/API-CONTRACTS-PT-BR.md).

## 🔐 Credenciais de Teste

| Email | Senha | Perfil | Permissões |
|-------|-------|--------|------------|
| `admin@investpro.com` | `password` | Admin | Total |
| `editor@investpro.com` | `password` | Editor | Criar/Editar |
| `viewer@investpro.com` | `password` | Visualizador | Apenas leitura |

## 📦 Endpoints Disponíveis

### Autenticação
- `POST /api/v1/auth/login` - Login
- `POST /api/v1/auth/logout` - Logout
- `GET /api/v1/auth/me` - Usuário atual

### Usuários (Admin)
- `GET /api/v1/users` - Listar usuários
- `POST /api/v1/users` - Criar usuário
- `PATCH /api/v1/users/:id` - Atualizar usuário
- `DELETE /api/v1/users/:id` - Excluir usuário

### Portfolios
- `GET /api/v1/portfolios` - Listar portfolios
- `GET /api/v1/portfolios/:id` - Detalhes do portfolio
- `GET /api/v1/portfolios/:id/summary` - Resumo do portfolio
- `POST /api/v1/portfolios` - Criar portfolio
- `PATCH /api/v1/portfolios/:id` - Atualizar portfolio
- `DELETE /api/v1/portfolios/:id` - Excluir portfolio

### Investimentos
- `GET /api/v1/investments` - Listar investimentos (com filtros)
- `GET /api/v1/investments/:id` - Detalhes do investimento
- `GET /api/v1/investments/summary` - Resumo geral
- `GET /api/v1/investments/dividends` - Dividendos
- `POST /api/v1/investments/fixed-income` - Criar renda fixa
- `POST /api/v1/investments/variable-income` - Criar renda variável
- `PATCH /api/v1/investments/:id` - Atualizar investimento
- `DELETE /api/v1/investments/:id` - Excluir investimento

### Chat
- `POST /api/v1/chat` - Enviar mensagem ao assistente

## 🚀 Comandos

```bash
# Desenvolvimento (API real por padrão)
bun run dev

# Demonstração local com MSW, quando desejado
VITE_USE_MSW=true bun run dev

# Build para Web (GitHub Pages)
GITHUB_PAGES=true VITE_USE_MSW=false bun run build

# Build para Android
CAPACITOR=true VITE_USE_MSW=false bun run build
npx cap sync android
npx cap run android
```

## 🐛 Debugando

Para debugar requisições, adicione um handler catch-all temporário:

```typescript
// Em handlers.ts, no início do array
http.all('*', ({ request }) => {
  console.log('[MSW DEBUG]', request.method, request.url)
  return undefined // Passa para o próximo handler
}),
```

## ⚠️ Notas Importantes

1. **Service Worker**: O arquivo `mockServiceWorker.js` deve estar em `public/`
2. **HTTPS no Android**: O Capacitor usa `https://invest-dashboard` como origem
3. **Cookies**: HTTPOnly cookies funcionam normalmente com `credentials: 'include'`
4. **HMR**: Alterações nos handlers recarregam automaticamente no dev
