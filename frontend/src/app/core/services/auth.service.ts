import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { AuthResponse, LoginRequest, RegisterRequest } from '../models/models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private apiUrl = 'https://localhost:7001/api/auth'; // ⚠️ Cambia el puerto al que te dé dotnet run

  // Signal con el usuario actual (Angular 18 moderno)
  currentUser = signal<AuthResponse | null>(this.loadFromStorage());

  register(data: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/register`, data)
      .pipe(tap(res => this.saveSession(res)));
  }

  login(data: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, data)
      .pipe(tap(res => this.saveSession(res)));
  }

  logout(): void {
    localStorage.removeItem('connecthub_session');
    this.currentUser.set(null);
  }

  get token(): string | null {
    return this.currentUser()?.token ?? null;
  }

  get isLoggedIn(): boolean {
    return this.currentUser() !== null;
  }

  private saveSession(res: AuthResponse): void {
    localStorage.setItem('connecthub_session', JSON.stringify(res));
    this.currentUser.set(res);
  }

  private loadFromStorage(): AuthResponse | null {
    const raw = localStorage.getItem('connecthub_session');
    return raw ? JSON.parse(raw) : null;
  }
}
