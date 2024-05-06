package com.github.bcgov.keycloak.broker.oidc.corrections;

import org.jboss.logging.Logger;
import org.keycloak.broker.saml.SAMLIdentityProvider;
import org.keycloak.broker.saml.SAMLIdentityProviderConfig;
import org.keycloak.models.KeycloakSession;
import org.keycloak.saml.validators.DestinationValidator;
import org.keycloak.broker.provider.AuthenticationRequest;
import org.keycloak.broker.provider.IdentityBrokerException;
import org.keycloak.dom.saml.v2.protocol.AuthnRequestType;
import org.keycloak.events.EventBuilder;
import org.keycloak.models.KeyManager;
import org.keycloak.models.RealmModel;
import org.keycloak.protocol.oidc.OIDCLoginProtocol;

import jakarta.ws.rs.core.Response;
import jakarta.ws.rs.core.UriBuilder;
import jakarta.ws.rs.core.UriInfo;
import org.keycloak.protocol.saml.JaxrsSAML2BindingBuilder;
import org.keycloak.protocol.saml.SamlProtocol;
import org.keycloak.protocol.saml.SamlSessionUtils;
import org.keycloak.protocol.saml.preprocessor.SamlAuthenticationPreprocessor;
import org.keycloak.saml.SAML2AuthnRequestBuilder;
import org.keycloak.saml.SAML2NameIDPolicyBuilder;
import org.keycloak.saml.SAML2RequestedAuthnContextBuilder;
import org.keycloak.saml.common.constants.JBossSAMLURIConstants;
import org.keycloak.saml.processing.core.util.KeycloakKeySamlExtensionGenerator;
import org.apache.commons.lang3.StringUtils;

import java.util.ArrayList;
import java.util.Iterator;
import java.util.List;


public class CorrectionsIdentityProvider extends SAMLIdentityProvider {

  private final DestinationValidator destinationValidator;
  protected static final Logger logger = Logger.getLogger(CorrectionsIdentityProvider.class);


  public CorrectionsIdentityProvider(KeycloakSession session, SiteMinderSamlIdentityProviderConfig smSamlIdentityProviderConfig,
                                DestinationValidator destinationValidator) {

    super(session, smSamlIdentityProviderConfig, destinationValidator);

    this.destinationValidator = destinationValidator;
  }

  @Override
  public Response performLogin(AuthenticationRequest request) {

    try {

      logger.info("Attempting to login " + request.getAuthenticationSession().getClientScopes());
      RealmModel realm = request.getRealm();
      logger.info("Realm " + realm.getName());


      String destinationUrl = getConfig().getSingleSignOnServiceUrl();

      JaxrsSAML2BindingBuilder jaxrsSAML2BindingBuilder = new JaxrsSAML2BindingBuilder(session).relayState(
        request.getState().getEncoded());


      SAML2AuthnRequestBuilder authnRequestBuilder = getAuthnRequestBuilder(request, realm, destinationUrl);
      boolean postBinding = getConfig().isPostBindingAuthnRequest();

      signAuthnRequest(realm, authnRequestBuilder, jaxrsSAML2BindingBuilder, postBinding);

      AuthnRequestType authnRequestType = getAuthnRequest(request, authnRequestBuilder);

      destinationUrl = updateDestinationUrl(authnRequestType, destinationUrl);

      request.getAuthenticationSession().setClientNote(SamlProtocol.SAML_REQUEST_ID_BROKER, authnRequestType.getID());

      if (postBinding) {
        return jaxrsSAML2BindingBuilder.postBinding(authnRequestBuilder.toDocument()).request(destinationUrl);
      } else {
        return jaxrsSAML2BindingBuilder.redirectBinding(authnRequestBuilder.toDocument()).request(destinationUrl);
      }
    } catch (Exception e) {
      throw new IdentityBrokerException("Could not create authentication request.", e);
    }
  }

  private AuthnRequestType getAuthnRequest(AuthenticationRequest request,
                                           SAML2AuthnRequestBuilder authnRequestBuilder) {
    AuthnRequestType authnRequest = authnRequestBuilder.createAuthnRequest();

    for (Iterator<SamlAuthenticationPreprocessor> it = SamlSessionUtils.getSamlAuthenticationPreprocessorIterator(
      session); it.hasNext(); ) {
      authnRequest = it.next().beforeSendingLoginRequest(authnRequest, request.getAuthenticationSession());
    }
    return authnRequest;
  }

  private String updateDestinationUrl(AuthnRequestType authnRequest, String destinationUrl) {

    if (authnRequest.getDestination() != null) {
      destinationUrl = authnRequest.getDestination().toString();
    }

    return destinationUrl;
  }

  private void signAuthnRequest(RealmModel realm, SAML2AuthnRequestBuilder authnRequestBuilder,
                                JaxrsSAML2BindingBuilder jaxrsSAML2BindingBuilder, boolean postBinding) {

    if (getConfig().isWantAuthnRequestsSigned()) {
      KeyManager.ActiveRsaKey keys = session.keys().getActiveRsaKey(realm);
      String keyName = getConfig().getXmlSigKeyInfoKeyNameTransformer()
        .getKeyName(keys.getKid(), keys.getCertificate());

      jaxrsSAML2BindingBuilder.signWith(keyName, keys.getPrivateKey(), keys.getPublicKey(), keys.getCertificate())
        .signatureAlgorithm(getSignatureAlgorithm()).signDocument();

      if (!postBinding && getConfig().isAddExtensionsElementWithKeyInfo()) {
        authnRequestBuilder.addExtension(new KeycloakKeySamlExtensionGenerator(keyName));
      }
    }
  }

  private SAML2AuthnRequestBuilder getAuthnRequestBuilder(AuthenticationRequest request, RealmModel realm,
                                                          String destinationUrl) {

    final SAML2AuthnRequestBuilder saml2AuthnRequestBuilder = new SAML2AuthnRequestBuilder().assertionConsumerUrl(
        request.getRedirectUri()).destination(destinationUrl).issuer(getEntityId(request.getUriInfo(), realm))
      .forceAuthn(getConfig().isForceAuthn()).protocolBinding(getProtocolBinding())
      .nameIdPolicy(SAML2NameIDPolicyBuilder.format(getNameIdPolicyFormat()).setAllowCreate(getAllowCreate()))
      .attributeConsumingServiceIndex(getConfig().getAttributeConsumingServiceIndex())
      .requestedAuthnContext(getRequestedAuthnContext()).subject(getLoginHint(request));

 //   saml2AuthnRequestBuilder.addExtension(new FaAuthnExtensionGenerator((FaSamlIdentityProviderConfig) getConfig()));

    return saml2AuthnRequestBuilder;
  }

  private String getLoginHint(AuthenticationRequest request) {

    return getConfig().isLoginHint() ? request.getAuthenticationSession()
      .getClientNote(OIDCLoginProtocol.LOGIN_HINT_PARAM) : null;
  }

  private Boolean getAllowCreate() {
    Boolean allowCreate = null;

    if (getConfig().getConfig().get(SAMLIdentityProviderConfig.ALLOW_CREATE) == null || getConfig().isAllowCreate()) {
      allowCreate = Boolean.TRUE;
    }

    return allowCreate;
  }

  private SAML2RequestedAuthnContextBuilder getRequestedAuthnContext() {
    SAML2RequestedAuthnContextBuilder requestedAuthnContext = new SAML2RequestedAuthnContextBuilder().setComparison(
      getConfig().getAuthnContextComparisonType());

    getAuthnContextClassRefUris().forEach(requestedAuthnContext::addAuthnContextClassRef);
    getAuthnContextDeclRefUris().forEach(requestedAuthnContext::addAuthnContextDeclRef);

    return requestedAuthnContext;
  }

  private String getProtocolBinding() {
    String protocolBinding = JBossSAMLURIConstants.SAML_HTTP_REDIRECT_BINDING.get();

    if (getConfig().isPostBindingResponse()) {
      protocolBinding = JBossSAMLURIConstants.SAML_HTTP_POST_BINDING.get();
    }

    return protocolBinding;
  }

  private String getNameIdPolicyFormat() {
    String nameIdPolicyFormat = getConfig().getNameIDPolicyFormat();

    if (nameIdPolicyFormat == null) {
      nameIdPolicyFormat = JBossSAMLURIConstants.NAMEID_FORMAT_PERSISTENT.get();
    }

    return nameIdPolicyFormat;
  }

  private String getEntityId(UriInfo uriInfo, RealmModel realm) {
    String configEntityId = getConfig().getEntityId();

    if (configEntityId == null || configEntityId.isEmpty()) {
      return UriBuilder.fromUri(uriInfo.getBaseUri()).path("realms").path(realm.getName()).build().toString();
    } else {
      return configEntityId;
    }
  }

  private List<String> getAuthnContextClassRefUris() {
    List<String> result = new ArrayList<>(0);

    if (StringUtils.isNotBlank(getConfig().getAuthnContextClassRefs())) {
    //  result = Arrays.asList( LJW
   //     SerializationConverter.INSTANCE.getStringSerializedAs(getConfig().getAuthnContextClassRefs(),
     //     String[].class));
    }

    return result;
  }

  private List<String> getAuthnContextDeclRefUris() {
    List<String> result = new ArrayList<>(0);

    if (StringUtils.isNotBlank(getConfig().getAuthnContextDeclRefs())) {
   //   result = Arrays.asList( LJW
    //    SerializationConverter.INSTANCE.getStringSerializedAs(getConfig().getAuthnContextDeclRefs(), String[].class));
   //
    }

    return result;
  }

  @Override
  public Object callback(RealmModel realm, AuthenticationCallback callback, EventBuilder event) {

    return new SiteMinderSamlEndpoint(session, realm,this, getConfig(), callback, destinationValidator);
  }
}
