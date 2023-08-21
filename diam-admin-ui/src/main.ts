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



const renderApp = () => {
  const pinia = createPinia()
  const app = createApp(App);
  app.use(router);
  app.use(pinia);
  app.mount("#app");
};


KeyCloakService.CallLogin(renderApp);
