import './assets/main.css'
import "bootstrap/dist/css/bootstrap.css";
import { createApp } from 'vue'
import { createPinia } from 'pinia'
import KeyCloakService from "./security/KeycloakService";

import App from './App.vue'
import router from './router'
import 'bootstrap-icons/font/bootstrap-icons.css'
import "./scss/custom.scss";

import "bootstrap";
 import "bootstrap/dist/js/bootstrap.bundle.min.js";
import type { EnvironmentConfig } from './environments/environment-config.model';

 fetch('/assets/environment.json')
   .then((response) => response.json())
  .then((configMap: EnvironmentConfig) => {
    if (configMap) {
        console.log("Using config from environment.json")
    }

  })
  .catch((err) => {
    console.warn('Config error - revert to local %o', err);

  });

const renderApp = () => {
    	const pinia = createPinia()
  const app = createApp(App);
  app.use(router);
  app.use(pinia);
  app.mount("#app");
};


KeyCloakService.CallLogin(renderApp);
