FROM oven/bun:1.3.4 AS builder

WORKDIR /app
ARG VITE_API_URL=/
ARG VITE_USE_MSW=false
ENV VITE_API_URL=${VITE_API_URL}
ENV VITE_USE_MSW=${VITE_USE_MSW}

COPY package.json bun.lock ./
RUN bun install --frozen-lockfile

COPY . .
RUN bunx msw init public --save && bun run build

FROM nginx:1.27-alpine AS runtime
COPY --from=builder /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
