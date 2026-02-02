import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export type AuthUser = {
  id: string;
  email: string;
  firstName?: string | null;
  lastName?: string | null;
};

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);

  csrf() {
    return this.http.get('/auth/csrf', { withCredentials: true, responseType: 'text' });
  }

  me() {
    return this.http.get<AuthUser>('/auth/me', { withCredentials: true });
  }

  register(payload: { email: string; password: string; firstName?: string; lastName?: string }) {
    return this.http.post<AuthUser>('/auth/register', payload, { withCredentials: true });
  }

  login(payload: { email: string; password: string }) {
    return this.http.post<AuthUser>('/auth/login', payload, { withCredentials: true });
  }

  logout() {
    return this.http.post('/auth/logout', {}, { withCredentials: true, responseType: 'text' });
  }
}
