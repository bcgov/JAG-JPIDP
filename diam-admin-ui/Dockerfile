FROM public.ecr.aws/bitnami/nginx:1.24

WORKDIR /app

COPY ./dist /app/.
COPY diam-bcps-admin.conf /opt/bitnami/nginx/conf/server_blocks/

EXPOSE 8080
