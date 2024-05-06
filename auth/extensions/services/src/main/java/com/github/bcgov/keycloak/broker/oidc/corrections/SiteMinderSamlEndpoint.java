package com.github.bcgov.keycloak.broker.oidc.corrections;
import jakarta.ws.rs.core.Context;
import org.keycloak.broker.provider.IdentityProvider.AuthenticationCallback;
import org.keycloak.broker.saml.SAMLEndpoint;
import org.keycloak.broker.saml.SAMLIdentityProvider;
import org.keycloak.broker.saml.SAMLIdentityProviderConfig;
import org.keycloak.models.KeycloakSession;
import org.keycloak.models.RealmModel;
import org.keycloak.saml.validators.DestinationValidator;

public class SiteMinderSamlEndpoint extends SAMLEndpoint {

  @Context
  private KeycloakSession session;

  public SiteMinderSamlEndpoint(KeycloakSession session, RealmModel realm, SAMLIdentityProvider provider, SAMLIdentityProviderConfig config,
                        AuthenticationCallback callback, DestinationValidator destinationValidator) {

    super(session,  provider, config, callback, destinationValidator);
  }


}
