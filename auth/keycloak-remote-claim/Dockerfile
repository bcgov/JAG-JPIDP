FROM registry.redhat.io/rh-sso-7/sso76-openshift-rhel8:7.6-5


MAINTAINER Stanley Okeke <stanley.okeke@gov.bc.ca>


USER root


RUN mkdir -p /opt/eap/modules/com/squareup/okhttp3/main/

# custom claim plugins module for wildfly
COPY okhttp-3.9.0.jar /opt/eap/modules/com/squareup/okhttp3/main/
COPY module.xml /opt/eap/modules/com/squareup/okhttp3/main/
COPY okio-1.13.0.jar /opt/eap/modules/com/squareup/okhttp3/main/

# deployment
COPY configuration/standalone-openshift.xml $JBOSS_HOME/standalone/configuration/standalone-openshift.xml
COPY json-graphql-remote-claim.jar /opt/eap/standalone/deployments


RUN touch /opt/eap/standalone/deployments/json-graphql-remote-claim.jar.deploy


USER 185