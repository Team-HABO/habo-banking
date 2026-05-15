# Stage 1 — build
FROM node:20-alpine AS builder

WORKDIR /app

ARG VITE_AUTH_API_URL
ENV VITE_AUTH_API_URL=$VITE_AUTH_API_URL

ARG VITE_ACCOUNT_API_URL
ENV VITE_ACCOUNT_API_URL=$VITE_ACCOUNT_API_URL

ARG VITE_VIEW_API_URL
ENV VITE_VIEW_API_URL=$VITE_VIEW_API_URL

COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build

# Stage 2 — serve
FROM nginx:alpine

COPY --from=builder /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
