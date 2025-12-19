# Welcome to your Lovable project

## Project info

**URL**: https://lovable.dev/projects/REPLACE_WITH_PROJECT_ID

## How can I edit this code?

There are several ways of editing your application.

**Use Lovable**

Simply visit the [Lovable Project](https://lovable.dev/projects/REPLACE_WITH_PROJECT_ID) and start prompting.

Changes made via Lovable will be committed automatically to this repo.

**Use your preferred IDE**

If you want to work locally using your own IDE, you can clone this repo and push changes. Pushed changes will also be reflected in Lovable.

The only requirement is having Bun installed - [install Bun](https://bun.sh/docs/installation)

Follow these steps:

```sh
# Step 1: Clone the repository using the project's Git URL.
git clone <YOUR_GIT_URL>

# Step 2: Navigate to the project directory.
cd <YOUR_PROJECT_NAME>

# Step 3: Install the necessary dependencies.
bun install

# Step 4: Start the development server with auto-reloading and an instant preview.
bun run dev
```

**Edit a file directly in GitHub**

- Navigate to the desired file(s).
- Click the "Edit" button (pencil icon) at the top right of the file view.
- Make your changes and commit the changes.

**Use GitHub Codespaces**

- Navigate to the main page of your repository.
- Click on the "Code" button (green button) near the top right.
- Select the "Codespaces" tab.
- Click on "New codespace" to launch a new Codespace environment.
- Edit files directly within the Codespace and commit and push your changes once you're done.

## What technologies are used for this project?

This project is built with:

- Bun 1.3.4
- Vite 7.2.7
- TypeScript
- React 19.2.1
- shadcn-ui
- Tailwind CSS

## Docker Usage

Este projeto está pronto para Docker. Você pode fazer build e rodar localmente usando Docker ou Docker Compose.

> [!NOTE]
> Por padrão, o Docker build usa o **MSW (Mock Service Worker)** para simular a API em produção. Para conectar a uma API real, configure as variáveis de ambiente apropriadas.

### Usando Docker Compose (Recomendado)

```sh
# Build e inicia o container com MSW habilitado (padrão)
docker-compose up -d --build

# Para usar API real, edite docker-compose.yml:
# args:
#   VITE_USE_MSW: "false"
#   VITE_API_URL: "https://sua-api.com"
```

### Build Local Manual

```sh
# Build com MSW habilitado (para testes/demo)
docker build -t invest-dashboard .

# Build para produção com API real
docker build \
  --build-arg VITE_USE_MSW=false \
  --build-arg VITE_API_URL=https://sua-api.com \
  -t invest-dashboard .

# Rodar o container
docker run -p 8080:80 invest-dashboard
```

A aplicação estará disponível em `http://localhost:8080`.

## CI/CD com GitHub Actions

O projeto inclui o seguinte workflow:

1.  **Deploy para GitHub Pages**: Realiza o deploy automático do site estático para o GitHub Pages em cada push na branch `main`.

> [!TIP]
> Para o deploy funcionar corretamente com sua API, adicione o segredo `VITE_API_URL` nas configurações do seu repositório GitHub (Settings > Secrets and variables > Actions).
