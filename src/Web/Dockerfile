# Build from src/Web: docker build src/Web (compose.prod.yaml does this). Static files served by nginx behind Traefik.
FROM node:24-alpine AS build
WORKDIR /web
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:1.27-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /web/dist /usr/share/nginx/html
# /plugins.json from HOMEAPPS_PLUGINS at start (nginx runs every script in /docker-entrypoint.d).
COPY docker/40-plugins-json.sh /docker-entrypoint.d/40-plugins-json.sh
EXPOSE 80
