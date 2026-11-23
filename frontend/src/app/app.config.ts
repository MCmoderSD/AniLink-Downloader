import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { ActivatedRouteSnapshot, provideRouter, ViewTransitionInfo, withComponentInputBinding, withViewTransitions } from '@angular/router';

import { routes } from './app.routes';

function deepest(snapshot: ActivatedRouteSnapshot): ActivatedRouteSnapshot {
  let current: ActivatedRouteSnapshot = snapshot;

  while (current.firstChild) current = current.firstChild;

  return current;
}

function samePage(from: ActivatedRouteSnapshot, to: ActivatedRouteSnapshot): boolean {
  const before: ActivatedRouteSnapshot = deepest(from);
  const after: ActivatedRouteSnapshot = deepest(to);

  return before.routeConfig === after.routeConfig && JSON.stringify(before.params) === JSON.stringify(after.params);
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(),
    provideRouter(routes, withComponentInputBinding(), withViewTransitions({
      skipInitialTransition: true,
      onViewTransitionCreated: ({ transition, from, to }: ViewTransitionInfo): void => {
        if (samePage(from, to)) transition.skipTransition();
      }
    }))
  ]
};