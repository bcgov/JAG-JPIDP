import './assets/main.css'

import { createApp } from 'vue'
import { createPinia } from 'pinia'
import KeyCloakService from "./security/KeycloakService";

import App from './App.vue'
import router from './router'
import "bootstrap/dist/css/bootstrap.min.css"
import "./scss/custom.scss";
 import "bootstrap/dist/js/bootstrap.bundle.min.js";
import { ApprovalsApi, Configuration } from './generated/openapi';


const renderApp = () => {
    
  const app = createApp(App);
  app.use(router);
  app.use(createPinia);
  app.mount("#app");
};


KeyCloakService.CallLogin(renderApp);
