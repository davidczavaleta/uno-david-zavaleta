# Build context esperado: la carpeta frontend/.
FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
RUN npm run build

FROM nginx:alpine AS final
COPY nginx.conf /etc/nginx/conf.d/default.conf
# El application builder de Angular 22 publica en dist/frontend/browser.
COPY --from=build /app/dist/frontend/browser /usr/share/nginx/html
EXPOSE 80
