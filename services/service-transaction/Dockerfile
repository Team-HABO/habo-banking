FROM node:22-bookworm-slim

WORKDIR /app

COPY package*.json ./
RUN npm ci --include=dev

COPY . .
RUN npm run generate

CMD ["npm", "run", "consume"]
