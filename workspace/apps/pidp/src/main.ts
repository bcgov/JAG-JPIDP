import { enableProdMode } from '@angular/core';
import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';

import { APP_CONFIG, APP_DI_CONFIG, AppConfig } from './app/app.config';
import { AppModule } from './app/app.module';
import { environment } from './environments/environment';
import { EnvironmentConfig } from './environments/environment-config.model';

// The deployment pipeline provides the production environment configuration
// without requiring a rebuild by placing `environment.json` within the public
// assets folder, otherwise local development in/outside of a container relies
// on the local environment files.

fetch('/assets/environment.json')
  .then((response) => response.json())
  .then((configMap: EnvironmentConfig) => {
    let appConfig = APP_DI_CONFIG;
    if (configMap) {
      const { keycloakConfig, ...root } = configMap;
      appConfig = { ...appConfig, ...root };
      appConfig.keycloakConfig.config = keycloakConfig.config;
    }

    return appConfig;
  })
  .catch((err) => {
    console.warn('Config error - revert to local %o', err);
    return APP_DI_CONFIG;
  })
  .then((appConfig: AppConfig) => {
    if (environment.production) {
      enableProdMode();
    }

    // set the URL to be the host for multi-domain setup
    console.log("Window locations %o", window.location);
    console.log("App config %o", appConfig);

    if (environment.production) {
      appConfig.applicationUrl = window.location.origin;
      appConfig.configEndpoint = window.location.origin;
      appConfig.apiEndpoint = window.location.origin + '/api/v1';
    }

    console.log("App config %o", appConfig);

    platformBrowserDynamic([
      {
        provide: APP_CONFIG,
        useValue: appConfig,
      },
    ])
      .bootstrapModule(AppModule)
      .catch((err) => {
        console.error(err);
      });
  });
