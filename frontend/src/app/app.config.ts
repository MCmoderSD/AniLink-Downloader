import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter, withComponentInputBinding, withViewTransitions } from '@angular/router';

import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(),
    // Each navigation is a view transition: the page is animated into the next one instead of jumping (styles.scss).
    provideRouter(routes, withComponentInputBinding(), withViewTransitions({ skipInitialTransition: true }))
  ]
};