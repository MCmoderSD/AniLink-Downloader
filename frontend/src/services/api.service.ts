import { HttpClient, HttpParams } from '@angular/common/http';
import { inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

// The API is served by the same origin as the page, so there is no host to configure and no credentials to send.
// In development the dev server forwards /api to the backend (proxy.conf.json).
export const API_BASE: string = '/api';

export abstract class ApiService {

  protected readonly http: HttpClient = inject(HttpClient);

  protected constructor(private readonly route: string) { }

  protected get<T>(path: string = '', params?: HttpParams): Promise<T> {
    return firstValueFrom(this.http.get<T>(this.url(path), { params }));
  }

  protected post<T = void>(path: string, body: unknown = null): Promise<T> {
    return firstValueFrom(this.http.post<T>(this.url(path), body));
  }

  protected delete<T = void>(path: string = ''): Promise<T> {
    return firstValueFrom(this.http.delete<T>(this.url(path)));
  }

  protected url(path: string = ''): string {
    return path ? `${API_BASE}/${this.route}/${path}` : `${API_BASE}/${this.route}`;
  }
}
