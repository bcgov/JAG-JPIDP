package com.github.bcgov.keycloak.broker.oidc.corrections;

import org.keycloak.broker.saml.SAMLIdentityProviderConfig;
import org.keycloak.models.IdentityProviderModel;

public class SiteMinderSamlIdentityProviderConfig extends SAMLIdentityProviderConfig {

  public static final String SM_REQUESTED_ATTRIBUTES = "smRequestedAttributes";

  public SiteMinderSamlIdentityProviderConfig() {

    super();
  }


  public SiteMinderSamlIdentityProviderConfig(IdentityProviderModel identityProviderModel) {

    super(identityProviderModel);
  }
  public void setSmRequestedAttributes(String requestedAttributes) {

    getConfig().put(SM_REQUESTED_ATTRIBUTES, requestedAttributes);
  }

  public String geSmRequestedAttributes() {

    return getConfig().get(SM_REQUESTED_ATTRIBUTES);
  }
}
