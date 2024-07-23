docker run -d -p 8080:8080 -p 8787:8787 --entrypoint /opt/keycloak/bin/kc.sh --name sso-24 --env DEBUG_PORT='*:8787' --env KEYCLOAK_ADMIN=admin --env KEYCLOAK_ADMIN_PASSWORD=admin sso:24 start-dev --spi-theme-static-max-age=-1 --spi-theme-cache-themes=false --spi-theme-cache-templates=false --debug

echo "Run 'docker exec -it keycloak bash' in order to get into the container and make dynamic changes to the themes"
docker logs -f keycloak