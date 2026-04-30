FROM node:24-alpine

WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci --include=dev --no-audit --no-fund

COPY tsconfig.json ./
COPY src ./src

EXPOSE 4000

CMD ["npm", "start"]