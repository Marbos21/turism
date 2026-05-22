FROM node:20-alpine AS dev
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .

RUN chmod +x docker-entrypoint.sh

EXPOSE 4200
ENTRYPOINT ["./docker-entrypoint.sh"]
CMD ["npm", "start", "--", "--host", "0.0.0.0", "--poll", "2000", "--allowed-hosts", "all"]

FROM node:20-alpine AS build
WORKDIR /app
ARG MAPBOX_API_KEY=""

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN printf "export const environmentLocal = { mapboxToken: '%s' };\n" "$MAPBOX_API_KEY" > src/environments/environment.local.ts
RUN npx ng build --configuration production

FROM nginx:1.27-alpine AS prod
COPY --from=build /app/dist/familytrip-web/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
