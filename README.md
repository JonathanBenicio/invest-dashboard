# Invest Dashboard

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Build Status](https://github.com/JonathanBenicio/invest-dashboard/actions/workflows/deploy.yml/badge.svg)](https://github.com/JonathanBenicio/invest-dashboard/actions)
[![Node Version](https://img.shields.io/badge/node-%3E%3D%2018-brightgreen)]

## 📖 Overview
A modern, responsive investment dashboard built with **React 19**, **Vite**, **TypeScript**, and **shadcn‑ui**. It provides portfolio tracking, investment insights, and an admin panel. The app also supports mobile deployment via **Capacitor**.

## ✨ Features & Modules
- **Dashboard** – Overview charts and key metrics.
- **Portfolio** – View and manage assets.
- **Investments** – Detailed investment listings.
- **Admin** – User management and settings.
- **Tools** – Utilities such as CSV export, theme switcher, etc.

## 🛠️ Tech Stack
| Category | Technologies |
|----------|--------------|
| Frontend | React 19, Vite, TypeScript, Tailwind CSS, shadcn‑ui |
| State Management | Zustand |
| Data Fetching | @tanstack/react-query |
| UI Components | @radix-ui, lucide‑react, recharts |
| Forms & Validation | react‑hook‑form, zod |
| Mock API | MSW |
| Mobile | Capacitor (Android & iOS) |
| CI/CD | GitHub Actions (deploy.yml) |
| Containerization | Docker, Docker Compose |

## 🚀 Getting Started
### Prerequisites
- **Node.js** ≥ 18 (or **npm**/**npx**)
- **Docker** (optional, for containerized development)

### Local Development
```sh
# Clone the repository
git clone <YOUR_GIT_URL>
cd invest-dashboard

# Install dependencies
npm install   # or pnpm install

# Run the development server
npm run dev   # or npx vite
```
Open `http://localhost:5173` to view the app.

## 📱 Mobile Development
```sh
# Sync Capacitor plugins
npx cap sync   # or npm run cap sync

# Android
npx cap open android   # or npm run cap open android

# iOS
npx cap open ios   # or npm run cap open ios
```

## 🚀 CI/CD

The project uses GitHub Actions for automation. Both workflows are configured for **manual trigger**, allowing you to choose the branch before running.

### 🌐 GitHub Pages Deployment
Deploys the web application to GitHub Pages.
1. Go to **Actions** > **Deploy to GitHub Pages**.
2. Click **Run workflow** and select the branch.
3. The app will be available at your GitHub Pages URL.

### 📱 Android APK Build
Generates the Android APK using Capacitor.
1. Go to **Actions** > **Build Android APK**.
2. Click **Run workflow**.
3. Select the branch and the build type (`debug` or `release`).
4. Once finished, download the APK from the **Artifacts** section of the run summary.

> [!IMPORTANT]
> Ensure the `VITE_API_URL` secret is configured in your repository settings (**Settings > Secrets and variables > Actions**) for the build to point to the correct API.

## 🐳 Docker
```sh
# Build and run with MSW (default)
docker-compose up -d --build

# Use real API – edit docker-compose.yml:
#   VITE_USE_MSW=false
#   VITE_API_URL=https://your-api.com
```

## 📂 Project Structure
```
src/
├─ api/            # API clients, env config
├─ components/     # UI components
├─ hooks/          # Custom React hooks
├─ pages/          # Route pages (dashboard, portfolio, etc.)
├─ mocks/          # MSW handlers
├─ store/          # Zustand store
├─ main.tsx        # App entry point
└─ index.css       # Global styles
```

## 🤝 Contributing
1. Fork the repository.
2. Create a feature branch.
3. Run `npm run lint` and ensure all tests pass.
4. Open a Pull Request.

## 📄 License
This project is licensed under the MIT License – see the [LICENSE](LICENSE) file.
