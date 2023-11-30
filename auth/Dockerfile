FROM registry.redhat.io/rh-sso-7/sso76-openshift-rhel8:7.6-5

MAINTAINER lee.wright@gov.bc.ca

USER root

# custom claim plugins module for wildfly
COPY okhttp-3.9.0.jar /opt/eap/modules/com/squareup/okhttp3/main/
COPY module.xml /opt/eap/modules/com/squareup/okhttp3/main/
COPY okio-1.13.0.jar /opt/eap/modules/com/squareup/okhttp3/main/

# deployment for Justin REST SPI
COPY json-graphql-remote-claim.jar /opt/eap/standalone/deployments

# deployment for BCGOV Themes
COPY theme/ $JBOSS_HOME/themes/
COPY configuration/standalone-openshift.xml $JBOSS_HOME/standalone/configuration/standalone-openshift.xml
COPY keycloak-metrics-spi-2.5.3.jar /opt/eap/standalone/deployments
COPY bcgov-services-1.0.0.jar /opt/eap/standalone/deployments

RUN touch /opt/eap/standalone/deployments/keycloak-metrics-spi-2.5.3.jar.deploy
RUN touch /opt/eap/standalone/deployments/bcgov-services-1.0.0.jar.deploy
RUN touch /opt/eap/standalone/deployments/json-graphql-remote-claim.jar.deploy

RUN chown -R jboss:jboss $JBOSS_HOME/themes

RUN chmod -R 775 $JBOSS_HOME/themes

USER 1000
