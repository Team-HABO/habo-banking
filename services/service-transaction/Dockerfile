FROM node:24-alpine

WORKDIR /app

COPY package*.json ./

RUN npm ci --include=dev --ignore-scripts

COPY . .
RUN npm run generate

CMD ["npm", "run", "consume"]
