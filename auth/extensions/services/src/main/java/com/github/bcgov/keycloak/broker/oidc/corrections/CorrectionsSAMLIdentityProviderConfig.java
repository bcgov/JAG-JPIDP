package com.github.bcgov.keycloak.broker.oidc.corrections;

import org.keycloak.broker.saml.SAMLIdentityProviderConfig;
import org.keycloak.models.IdentityProviderModel;

public class CorrectionsSAMLIdentityProviderConfig  extends SAMLIdentityProviderConfig {

    public static final String REQUESTED_ATTRIBUTES = "requestedAttributes";
    public CorrectionsSAMLIdentityProviderConfig() {
    }

    public CorrectionsSAMLIdentityProviderConfig(IdentityProviderModel identityProviderModel) {
        super(identityProviderModel);
    }

    public void setRequestedAttributes(String requestedAttributes) {
        getConfig().put(REQUESTED_ATTRIBUTES, requestedAttributes);
    }

    public String getRequestedAttributes() {
        return getConfig().get(REQUESTED_ATTRIBUTES);
    }
}
