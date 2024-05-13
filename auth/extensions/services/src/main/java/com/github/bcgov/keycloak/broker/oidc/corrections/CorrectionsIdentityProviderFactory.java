package com.github.bcgov.keycloak.broker.oidc.corrections;

import org.keycloak.Config;
import org.keycloak.broker.saml.SAMLIdentityProvider;
import org.keycloak.broker.saml.SAMLIdentityProviderFactory;
import org.keycloak.models.IdentityProviderModel;
import org.keycloak.models.KeycloakSession;
import org.keycloak.saml.validators.DestinationValidator;

public class CorrectionsIdentityProviderFactory extends SAMLIdentityProviderFactory {

  public static final String PROVIDER_ID = "ag-corrections";
  private DestinationValidator destinationValidator;

  @Override
  public String getName() {
    return "AG Corrections BioMetrics - Custom";
  }

  @Override
  public void init(Config.Scope config) {

    super.init(config);

    this.destinationValidator = DestinationValidator.forProtocolMap(config.getArray("knownProtocols"));
  }

  @Override
  public CorrectionsSAMLIdentityProviderConfig createConfig() {
    return new CorrectionsSAMLIdentityProviderConfig();
  }

  @Override
  public SAMLIdentityProvider create(KeycloakSession session, IdentityProviderModel model) {

    return new CorrectionsIdentityProvider(session, new CorrectionsSAMLIdentityProviderConfig(model), destinationValidator);

  }

  @Override
  public String getId() {
    return PROVIDER_ID;
  }
}
