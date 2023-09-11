import Keycloak from "keycloak-js";

const keycloakInstance = new Keycloak();
interface CallbackOneParam<T1 = void, T2 = void> {
  (param1: T1): T2;
}
/**
 * Initializes Keycloak instance and calls the provided callback function if successfully authenticated.
 *
 * @param onAuthenticatedCallback
 */
const Login = (onAuthenticatedCallback: CallbackOneParam) => {
  keycloakInstance
    .init({ onLoad: "login-required" })
    .then(function (authenticated) {
      debugger;
      const proc = process || null;
      const environ = import.meta.env;
      const client = import.meta.env.VITE_KEYCLOAK_CLIENT;
      const roles = keycloakInstance.resourceAccess?.[client].roles;
      console.log(roles);
      if (! (roles?.includes('ADMIN') || roles?.includes('APPROVER')))
      {
        alert("unauthorized");

      } else
      {
      authenticated ? onAuthenticatedCallback() : alert("non authenticated");

      }

    })
    .catch((e) => {
      console.dir(e);
      console.log(`keycloak init exception: ${e}`);
    });
};

const Logout = () => {
      const logoutOptions = { redirectUri : 'http://127.0.0.1:5173/' };

    keycloakInstance.logout(logoutOptions).then((success) => {
            console.log("--> log: logout success ", success );
    }).catch((error) => {
            console.log("--> log: logout error ", error );
    });
}


const UserName = (): string | undefined =>
  keycloakInstance?.tokenParsed?.preferred_username;

const Token = (): string | undefined => keycloakInstance?.token;

const KeyCloakService = {
  CallLogin: Login,
  GetToken: Token,
  GetUserName: UserName,
  CallLogout: Logout
};

export default KeyCloakService;