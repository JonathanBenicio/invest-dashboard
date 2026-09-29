# Original User Request

## Initial Request — 2026-06-04T00:30:02-03:00

Integrate the .NET 10 backend with the React 19 frontend of the invest-dashboard monorepo, removing all MSW mocks as the default data source and connecting to real backend APIs. This is a production-grade integration — all endpoints must work with real data through a fully functional auth pipeline.

Working directory: c:\Users\Jonathan\Documents\Developer\GitHub\invest-dashboard
Integrity mode: development

## Requirements

### R1. Auth Controller — Backend Gateway with Provider Abstraction

The backend must become the single authentication gateway. Create an `AuthController` (`api/v1/auth`) that proxies authentication through an `IAuthProvider` interface. The initial implementation (`SupabaseAuthProvider`) must call the Supabase Admin API server-side.

**Endpoints required:**
- `POST /auth/login` — email/password login
- `POST /auth/register` — user registration
- `POST /auth/logout` — session invalidation
- `POST /auth/refresh` — refresh access token
- `GET /auth/me` — current user info
- `PATCH /auth/me` — update profile
- `POST /auth/oauth/{provider}` — initiate OAuth flow (Google, GitHub)
- `GET /auth/oauth/callback` — handle OAuth callback

**Token strategy:**
- Access token: JWT, 15 min expiration
- Refresh token: opaque, 7 days expiration, no rotation
- Dual delivery: HttpOnly cookie for web + Authorization Bearer header for mobile/API
- The backend must accept auth from either mechanism

**Architecture:**
- `IAuthProvider` interface in Application layer (methods: `LoginAsync`, `RegisterAsync`, `RefreshAsync`, `GetUserAsync`, `OAuthLoginAsync`, etc.)
- `SupabaseAuthProvider` implementation in Infrastructure layer (uses Supabase Admin API via HTTP)
- The backend issues its OWN JWTs after validating with Supabase — it does NOT forward Supabase tokens to the frontend

### R2. Frontend Auth Refactor — Remove Supabase SDK

The frontend must stop using `@supabase/supabase-js` for authentication. All auth calls go through the backend API client instead.

**Changes required:**
- Replace `supabase.auth.*` calls in `authStore.ts` with API client calls to `/auth/*` endpoints
- Remove `supabase.ts` lib file (or keep only if needed for non-auth features)
- Update `AuthGuard` and `AuthInitializer` to use the new auth flow
- API client (`client.ts`) must attach the JWT: as a Bearer header from stored token
- Implement automatic token refresh when access token expires (intercept 401, call `/auth/refresh`, retry)
- Store tokens appropriately: access token in memory (Zustand), refresh token received via HttpOnly cookie automatically

### R3. CORS + API Routing Fixes

- Configure CORS in the backend `Program.cs` to accept requests from the frontend origin (`http://localhost:5173` for dev, configurable for production)
- Fix the double `/api/v1` prefix bug in `frontend/src/api/services/taxes.service.ts` and `frontend/src/api/services/simulation.service.ts` — these currently produce `/api/v1/api/v1/taxes`

### R4. Remove Hardcoded Fallbacks from Controllers

Remove all simulated/hardcoded data fallbacks in backend controllers. When no data exists, return proper HTTP responses:
- `CarteirasController`: remove simulated portfolio and summary data — return empty list or 404
- `InvestimentosController`: remove simulated positions, summary, and hardcoded dividends — return empty responses
- `PATCH` operations must actually persist changes via EF Core
- `DELETE` operations must actually delete records
- The frontend must handle empty states gracefully (show empty state UI, not errors)

### R5. MSW Default-Off Configuration

MSW must be disabled by default in development. It should only activate when explicitly enabled via `VITE_USE_MSW=true`. This ensures developers use the real backend by default.

### R6. SignalR Frontend Integration

Connect the frontend to the existing `DadosMercadoHub` (`/hubs/market-data`) for real-time price updates.
- Install `@microsoft/signalr` in the frontend
- Create a SignalR service/hook that connects to the hub, subscribes to tickers, and receives price updates
- Use the new JWT for authentication (pass via query string `access_token` as the backend expects)
- Integrate real-time prices into the relevant investment/portfolio views

## Acceptance Criteria

### Auth Integration
- [ ] `POST /api/v1/auth/login` with valid email/password returns access token + sets refresh token cookie
- [ ] `POST /api/v1/auth/register` creates a new user and returns tokens
- [ ] `POST /api/v1/auth/refresh` returns new access token when valid refresh token cookie is present
- [ ] `GET /api/v1/auth/me` returns user info when valid JWT is provided (cookie OR Bearer)
- [ ] `POST /api/v1/auth/oauth/google` initiates OAuth flow correctly
- [ ] Frontend login flow works end-to-end without any Supabase SDK calls
- [ ] Frontend automatically refreshes expired access tokens transparently
- [ ] `@supabase/supabase-js` is no longer imported in auth-related code

### API Connectivity
- [ ] All frontend API calls reach the real backend when `VITE_USE_MSW` is not set or set to `false`
- [ ] `taxes.service.ts` and `simulation.service.ts` produce correct URLs (no double `/api/v1`)
- [ ] CORS headers allow frontend origin in development and production configurations

### Backend Data Integrity
- [ ] `CarteirasController` returns empty list (not simulated data) when user has no portfolios
- [ ] `InvestimentosController` returns empty list (not simulated positions) when portfolio is empty
- [ ] Dividends endpoint returns real data from DB (not 5 hardcoded items)
- [ ] `PATCH /portfolios/:id` persists changes to database
- [ ] `DELETE /portfolios/:id` actually removes the record

### Frontend Empty States
- [ ] Dashboard shows meaningful empty state when no investments exist
- [ ] Portfolio page shows empty state with "create portfolio" CTA when no portfolios exist
- [ ] Investment lists show empty state when no investments of that type exist

### SignalR
- [ ] Frontend connects to `/hubs/market-data` using the new JWT
- [ ] Price updates are received in real-time when subscribed to tickers
- [ ] Connection reconnects automatically on disconnect

### Build & Tests
- [ ] `cd frontend && bun run build` completes without errors
- [ ] `cd src && dotnet build InvestDashboard.slnx` completes without errors
- [ ] `cd src && dotnet test` — all existing unit tests pass
- [ ] New integration tests exist for: auth login flow, auth refresh flow, portfolio CRUD, investment CRUD
- [ ] `cd frontend && bun run test` passes (if E2E tests exist)

### MSW Configuration
- [ ] Running `cd frontend && bun run dev` without `VITE_USE_MSW=true` does NOT activate MSW
- [ ] Running `VITE_USE_MSW=true bun run dev` activates MSW with all mock handlers
