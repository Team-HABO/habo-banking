FROM node:24-alpine

WORKDIR /app

COPY package*.json ./

RUN npm ci --include=dev --ignore-scripts

COPY . .
RUN npm run generate

CMD ["sh", "-c", "npx prisma migrate deploy && npx prisma db seed && npm run consume"]
