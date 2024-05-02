FROM node:lts-alpine as build

WORKDIR /app

# Copy the package.json and install dependencies
COPY package*.json /app/

RUN npm install

COPY ./ /app/

RUN npm run build

RUN ls -l /

FROM docker.io/bitnami/nginx:1.25

COPY --from=build /app/dist /app/.
COPY diam-bcps-admin.conf /opt/bitnami/nginx/conf/server_blocks/

EXPOSE 8080
