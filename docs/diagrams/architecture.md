# Arquitetura do Sistema

Este documento descreve a topologia em alto nível do sistema usando a abstração do C4 Model.

## C4 Model - Diagrama de Contexto & Containers

```mermaid
graph TD
    User([Investidor])
    
    subgraph Frontend ["Frontend App (React 19 / Vite / Capacitor)"]
        Web[Dashboard Web]
        Mobile[Aplicativo Mobile nativo]
    end

    subgraph Backend ["Backend Environment (.NET 10 / DDD)"]
        API[ASP.NET Core WebAPI]
        Domain[Core Domain Model]
        Worker[Market Data Synchronizer Worker]
        Hub[SignalR Real-time Hub]
    end

    subgraph External ["External Managed Services"]
        Supabase[(Supabase PostgreSQL)]
        Auth[Supabase Auth Provider]
        MarketAPI[Market Quotation APIs / Oráculos]
    end

    User -->|Interage via Navegador| Web
    User -->|Interage via App| Mobile
    
    Web -->|REST API (HTTPS)| API
    Mobile -->|REST API (HTTPS)| API
    
    Web -->|WebSockets| Hub
    Mobile -->|WebSockets| Hub

    API -->|Validação de JWT| Auth
    API -->|Entity Framework Core| Supabase
    
    Worker -->|Fetch Ticker Prices| MarketAPI
    Worker -->|Envia Broadcast Update| Hub
    
    API -.->|Injeta e executa| Domain
    Worker -.->|Injeta e atualiza| Domain
```

## Sequence Diagram - Fluxo de Compra de Ativos (Backend DDD)

Este diagrama ilustra o fluxo de uma requisição de transação financeira, destacando a separação de responsabilidades (WebAPI -> Application -> Domain -> Infrastructure).

```mermaid
sequenceDiagram
    autonumber
    actor User as Investidor
    participant Web as Frontend (React)
    participant API as WebAPI (.NET)
    participant Domain as Core Domain (DDD)
    participant DB as EF Core / PostgreSQL
    
    User->>Web: Preenche formulário (Ex: Compra 100x AAPL34)
    Web->>API: POST /api/transactions + JWT
    API->>API: Valida JWT e extrai UserID
    API->>Domain: Envia comando (CreateTransactionCommand)
    
    rect rgb(240, 248, 255)
        note right of Domain: Regras de Negócio e Cálculos
        Domain->>DB: Busca Posição Atual (AssetPosition)
        DB-->>Domain: Retorna Posição (ou cria nova em memória)
        Domain->>Domain: Aplica Transação (Soma cotas)
        Domain->>Domain: Recalcula Preço Médio
    end
    
    Domain->>DB: SaveChangesAsync()
    DB-->>Domain: Confirmação do Banco
    Domain-->>API: Retorna DTO de Sucesso
    API-->>Web: HTTP 200 OK
    Web-->>User: Toast "Transação salva" e atualiza listagem
```

## Sequence Diagram - Atualizações em Tempo Real (SignalR)

Este diagrama mostra o ciclo de vida da captura de preços de mercado até a renderização no frontend via WebSockets.

```mermaid
sequenceDiagram
    participant Market as Oráculo/API de Mercado
    participant Worker as Background Service
    participant Hub as SignalR Hub
    participant Web as Frontend (React/Zustand)
    
    Web->>Hub: Abre conexão WebSocket (Handshake)
    
    loop A cada X minutos
        Worker->>Market: Http Get (Lista de Tickers Ativos)
        Market-->>Worker: Retorna Cotações Atualizadas
        Worker->>Hub: Envia evento "ReceiveMarketUpdate"
        Hub-->>Web: Push Server-Sent Event (WebSocket)
        Web->>Web: Zustand Store mapeia novos preços
        Web->>Web: React re-renderiza componentes dependentes
    end
```
