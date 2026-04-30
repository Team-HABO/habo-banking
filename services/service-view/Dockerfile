FROM node:22-alpine

WORKDIR /app
RUN chown -R node:node /app

USER node

COPY --chown=node:node package.json package-lock.json ./
RUN npm install --include=dev --no-audit --no-fund

COPY --chown=node:node tsconfig.json ./
COPY --chown=node:node src ./src

EXPOSE 4000

CMD ["npm", "start"]