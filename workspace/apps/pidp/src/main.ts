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

try {
  fetch('/assets/environment.json')
    .then((res) => console.log('Loaded environment.json %o', res))
    .catch((err) => console.log('Failed to load environment %o', err));
} catch (err) {
  console.log('No environment file located - using dev defaults');
}

fetch('/assets/environment.json')
  .then((response) => response.json())
  .then((configMap: EnvironmentConfig) => {
    let appConfig = APP_DI_CONFIG;

    if (configMap) {
      const { keycloakConfig, ...root } = configMap;
      appConfig = { ...appConfig, ...root };
      appConfig.keycloakConfig.config = keycloakConfig.config;
    }

    console.log('Using api url %s', appConfig.apiEndpoint);
    console.log('Using app url %s', appConfig.applicationUrl);

    return appConfig;
  })
  .catch(() => APP_DI_CONFIG)
  .then((appConfig: AppConfig) => {
    if (environment.production) {
      enableProdMode();
    }

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
