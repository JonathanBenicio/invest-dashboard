# Stage 1: Build the application
FROM oven/bun:1 AS builder

WORKDIR /app

# Pass build arguments for environment variables
ARG VITE_API_URL
ARG VITE_USE_MSW=true
ENV VITE_API_URL=$VITE_API_URL
ENV VITE_USE_MSW=$VITE_USE_MSW

COPY package.json bun.lock ./
RUN --mount=type=cache,target=/root/.bun/install/cache \
    bun install --frozen-lockfile

COPY . .
RUN bun run build

# Stage 2: Serve the application
FROM nginx:alpine

COPY --from=builder /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
