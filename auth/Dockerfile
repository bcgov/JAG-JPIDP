FROM image-registry.apps.gold.devops.gov.bc.ca/e27db1-tools/kc_rhel9:22-6

MAINTAINER lee.wright@gov.bc.ca

USER root

# custom claim plugins module for wildfly
COPY okhttp-3.9.0.jar /opt/keycloak/lib/lib/main/
COPY module.xml /opt/keycloak/lib/lib/main/
COPY okio-1.13.0.jar /opt/keycloak/lib/lib/main/

# deployment for Justin REST SPI
COPY json-graphql-remote-claim.jar /opt/keycloak/providers

# deployment for BCGOV Themes
COPY theme/ /opt/keycloak/themes/
##COPY configuration/standalone-openshift.xml $JBOSS_HOME/standalone/configuration/standalone-openshift.xml
#COPY keycloak-metrics-spi-2.5.3.jar /opt/keycloak/providers
#COPY bcgov-services-1.0.0.jar /opt/keycloak/providers

#RUN touch /opt/eap/standalone/deployments/keycloak-metrics-spi-2.5.3.jar.deploy
#RUN touch /opt/eap/standalone/deployments/bcgov-services-1.0.0.jar.deploy
#RUN touch /opt/eap/standalone/deployments/json-graphql-remote-claim.jar.deploy

#RUN chown -R jboss:jboss $JBOSS_HOME/themes

#RUN chmod -R 775 $JBOSS_HOME/themes
COPY entrypoint.sh /opt/keycloak/bin/entrypoint.sh
RUN chgrp 0 /opt/keycloak/bin/entrypoint.sh && chmod a+rx,o-rx /opt/keycloak/bin/entrypoint.sh


USER 1000

ENTRYPOINT [ "/opt/keycloak/bin/entrypoint.sh","/opt/keycloak/bin/kc.sh" ]
