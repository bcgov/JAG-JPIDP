package com.github.bcgov.keycloak.authenticators.csnumber;

import jakarta.ws.rs.core.MultivaluedMap;
import jakarta.ws.rs.core.Response;
import jakarta.ws.rs.core.UriBuilder;
import org.keycloak.authentication.AuthenticationFlowContext;
import org.keycloak.authentication.AuthenticationFlowError;
import org.keycloak.authentication.Authenticator;
import org.keycloak.forms.login.LoginFormsProvider;
import org.keycloak.forms.login.freemarker.FreeMarkerLoginFormsProvider;
import org.keycloak.models.*;
import org.keycloak.models.utils.FormMessage;
import org.keycloak.sessions.AuthenticationSessionModel;

/**
 * Author lee.wright@gov.bc.ca
 */
public class CSNumberAuthenticator implements Authenticator {

  private static final String RESULT_FIELD = "csnumber.result";
  private static final String ERROR_MESSAGE = "csnumber.result.error";
  private static final String TEMPLATE = "csnumberform.ftl";


  @Override
  public void authenticate(AuthenticationFlowContext context) {
    System.out.println("authenticate");
      Response response = context.form().createForm(TEMPLATE);
      context.challenge(response);
  }

  public void action(AuthenticationFlowContext context) {
 System.out.println("action");
      MultivaluedMap<String, String> formParameters = context.getHttpRequest().getDecodedFormParameters();
      String enteredResult = formParameters.getFirst("login");

      if (enteredResult != null) {
          StringBuilder enteredCSNumber = new StringBuilder();

          for ( int i = 1; i < formParameters.size(); i++){
              String key = "cs_num_"+i;
              String value = formParameters.get(key).get(0);
              enteredCSNumber.append(value);
          }

          if(enteredCSNumber.isEmpty()) {
              FormMessage errorMessage = new FormMessage(ERROR_MESSAGE);
              Response response = prepareCSNumber(context, errorMessage);
              context.failureChallenge(AuthenticationFlowError.INVALID_CREDENTIALS, response);
          }
          context.resetFlow();

          int result = Integer.parseInt(enteredCSNumber.toString());
          System.out.println(result);
          System.out.println(context.getRefreshExecutionUrl());

          UriBuilder loginUriBuilder = UriBuilder.fromUri("https://bmhub.test.biometrics.gov.bc.ca/BM12/Authenticate/Authenticate.aspx")
                  .queryParam("cs_number", result);
          Response response = Response.seeOther(loginUriBuilder.build()).build();
          context.forceChallenge(response);

          System.out.println(context.getRefreshExecutionUrl());
          return;
      }
  }

  private static int getRandomNumber(int lowerBound, int upperBound) {
    return (int) ((Math.random() * (upperBound - lowerBound)) + lowerBound);
  }
  private static Response prepareCSNumber(AuthenticationFlowContext context, FormMessage errorMessage) {
    //AuthenticatorConfigModel authenticatorConfig = context.getAuthenticatorConfig();
    //Map<String, String> config = authenticatorConfig.getConfig();

    AuthenticationSessionModel authSession = context.getAuthenticationSession();
    authSession.setAuthNote(RESULT_FIELD, Integer.toString(12345678));
    LoginFormsProvider formsProvider = new FreeMarkerLoginFormsProvider(context.getSession());
    return formsProvider.createForm(TEMPLATE);
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