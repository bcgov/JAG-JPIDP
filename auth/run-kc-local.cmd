docker stop keycloak
docker rm keycloak
docker run -p 8080:8080 --entrypoint /opt/keycloak/bin/kc.sh --name keycloak --env KEYCLOAK_ADMIN=admin --env KEYCLOAK_ADMIN_PASSWORD=admin sso:24 start-dev --spi-theme-static-max-age=-1 --spi-theme-cache-themes=false --spi-theme-cache-templates=false