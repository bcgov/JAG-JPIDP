package com.github.bcgov.keycloak.authenticators.csnumber;

import jakarta.ws.rs.core.MultivaluedMap;
import jakarta.ws.rs.core.Response;
import org.keycloak.authentication.AuthenticationFlowContext;
import org.keycloak.authentication.AuthenticationFlowError;
import org.keycloak.authentication.Authenticator;
import org.keycloak.forms.login.LoginFormsProvider;
import org.keycloak.forms.login.freemarker.FreeMarkerLoginFormsProvider;
import org.keycloak.models.AuthenticatorConfigModel;
import org.keycloak.models.KeycloakSession;
import org.keycloak.models.RealmModel;
import org.keycloak.models.UserModel;
import org.keycloak.models.utils.FormMessage;
import org.keycloak.sessions.AuthenticationSessionModel;

import java.util.Map;

/**
 * Author lee.wright@gov.bc.ca
 */
public class CSNumberAuthenticator implements Authenticator {

  private static final String RESULT_FIELD = "csnumber.result";
  private static final String ERROR_MESSAGE = "csnumber.result.error";
  private static final String TEMPLATE = "csnumberform.ftl";

  @Override
  public void authenticate(AuthenticationFlowContext context) {
    //Response response = prepareCSNumber(context, null);
    Response response = context.form().createForm(TEMPLATE);
    context.challenge(response);
  }

  @Override
  public void action(AuthenticationFlowContext context) {
    AuthenticationSessionModel authSession = context.getAuthenticationSession();
    String mathResult = authSession.getAuthNote(RESULT_FIELD);
    int result = Integer.parseInt(mathResult);

    MultivaluedMap<String, String> formParameters = context.getHttpRequest().getDecodedFormParameters();
    String enteredResult = formParameters.getFirst("result");
    int enteredResultNumber = Integer.parseInt(enteredResult);

    if (result == enteredResultNumber) {
      context.success();
    } else {
      FormMessage errorMessage = new FormMessage(ERROR_MESSAGE);
      Response response = prepareCSNumber(context, errorMessage);
      context.failureChallenge(AuthenticationFlowError.INVALID_CREDENTIALS, response);
    }
  }

  private static Response prepareCSNumber(AuthenticationFlowContext context, FormMessage errorMessage) {
    AuthenticatorConfigModel authenticatorConfig = context.getAuthenticatorConfig();
    //Map<String, String> config = authenticatorConfig.getConfig();

    AuthenticationSessionModel authSession = context.getAuthenticationSession();
    authSession.setAuthNote(RESULT_FIELD, Integer.toString(12345678));
    LoginFormsProvider formsProvider = new FreeMarkerLoginFormsProvider(context.getSession());
    return formsProvider.createForm(TEMPLATE);
  }

  private static int getRandomNumber(int lowerBound, int upperBound) {
    return (int) ((Math.random() * (upperBound - lowerBound)) + lowerBound);
  }

  @Override
  public boolean requiresUser() {
    return false;
  }

  @Override
  public boolean configuredFor(KeycloakSession session, RealmModel realm, UserModel user) {
    return true;
  }

  @Override
  public void setRequiredActions(KeycloakSession session, RealmModel realm, UserModel user) {
  }

  @Override
  public void close() {
  }

}