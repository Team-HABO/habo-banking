FROM node:22-alpine

WORKDIR /app

COPY package.json package-lock.json ./
RUN npm install --include=dev --no-audit --no-fund

COPY tsconfig.json ./
COPY src ./src

EXPOSE 4000

CMD ["npm", "start"]