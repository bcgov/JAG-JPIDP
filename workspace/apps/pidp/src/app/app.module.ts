import { NgModule, Injector } from '@angular/core';

import { AppRoutingModule } from '@app/app-routing.module';
import { AppComponent } from '@app/app.component';

import { CoreModule } from '@core/core.module';
export let AppInjector: Injector;

@NgModule({
  declarations: [AppComponent],
  imports: [AppRoutingModule, CoreModule],
  bootstrap: [AppComponent]

})
export class AppModule {
  // eslint-disable-next-line @typescript-eslint/explicit-member-accessibility
  constructor(private injector: Injector) {
    AppInjector = this.injector;
  }
}
