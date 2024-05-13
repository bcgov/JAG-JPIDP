package com.github.bcgov.keycloak.authenticators.csnumber;

import org.keycloak.Config;
import org.keycloak.authentication.Authenticator;
import org.keycloak.authentication.AuthenticatorFactory;
import org.keycloak.models.KeycloakSession;
import org.keycloak.models.KeycloakSessionFactory;
import org.keycloak.provider.ProviderConfigProperty;
import org.keycloak.models.AuthenticationExecutionModel;
import java.util.List;

public class CSNumberAuthenticatorFactory  implements AuthenticatorFactory {

  public static final String PROVIDER_ID = "corrections-csnumber";

  private String lowerBound;
  private String upperBound;
  private String csNumber;

  private final Authenticator SINGLETON = new CSNumberAuthenticator();


  @Override
  public Authenticator create(KeycloakSession session) {
    return SINGLETON;
  }

  @Override
  public String getDisplayType() {
    return "TEST Corrections CS Number / BioMetrics Authenticator";
  }

  @Override
  public String getReferenceCategory() {
    return PROVIDER_ID;
  }

  @Override
  public boolean isConfigurable() {
    return true;
  }

  @Override
  public AuthenticationExecutionModel.Requirement[] getRequirementChoices() {
    return REQUIREMENT_CHOICES;
  }

  @Override
  public boolean isUserSetupAllowed() {
    return false;
  }

  @Override
  public String getHelpText() {
    return "";
  }

  @Override
  public List<ProviderConfigProperty> getConfigProperties() {
    return List.of(
      new ProviderConfigProperty("csNumber", "Client Services Number", "", ProviderConfigProperty.STRING_TYPE, this.csNumber)
    );
  }

  @Override
  public void init(Config.Scope config) {
    this.lowerBound = config.get("lowerBound", "0");
    this.upperBound = config.get("upperBound", "20");
  }

  @Override
  public void postInit(KeycloakSessionFactory factory) {
  }

  @Override
  public void close() {
  }

  @Override
  public String getId() {
    return PROVIDER_ID;
  }

}
