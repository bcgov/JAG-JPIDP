FROM registry.redhat.io/rh-sso-7/sso76-openshift-rhel8:7.6-32

MAINTAINER lee.wright@gov.bc.ca

USER root

COPY theme/ $JBOSS_HOME/themes/
COPY configuration/standalone-openshift.xml $JBOSS_HOME/standalone/configuration/standalone-openshift.xml
COPY keycloak-metrics-spi-2.5.3.jar /opt/eap/standalone/deployments
COPY bcgov-services-1.0.0.jar /opt/eap/standalone/deployments

RUN touch /opt/eap/standalone/deployments/keycloak-metrics-spi-2.5.3.jar.deploy
RUN touch /opt/eap/standalone/deployments/bcgov-services-1.0.0.jar.deploy

RUN chown -R jboss:jboss $JBOSS_HOME/themes

RUN chmod -R 775 $JBOSS_HOME/themes

USER 1000
