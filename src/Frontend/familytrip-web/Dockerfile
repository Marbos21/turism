FROM node:20-alpine AS dev
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .

RUN chmod +x docker-entrypoint.sh

EXPOSE 4200
ENTRYPOINT ["./docker-entrypoint.sh"]
CMD ["npm", "start", "--", "--host", "0.0.0.0", "--poll", "2000", "--allowed-hosts", "all"]
