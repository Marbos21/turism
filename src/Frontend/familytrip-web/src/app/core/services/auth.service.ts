import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  User, LoginRequest, RegisterRequest, AuthResponse
} from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly TOKEN_KEY   = 'ft_access_token';
  private readonly USER_KEY    = 'ft_user';
  private readonly EXPIRES_KEY = 'ft_expires_at';

  private readonly _currentUser = signal<User | null>(this.loadStoredUser());
  readonly currentUser = this._currentUser.asReadonly();
  readonly isLoggedIn  = computed(() => this._currentUser() !== null && !this.isTokenExpired());

  constructor(
    private http: HttpClient,
    private router: Router
  ) {}

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/login`, request).pipe(
      tap(response => this.storeSession(response)),
      catchError(err => throwError(() => err))
    );
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/register`, request).pipe(
      tap(response => this.storeSession(response)),
      catchError(err => throwError(() => err))
    );
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    localStorage.removeItem(this.EXPIRES_KEY);
    this._currentUser.set(null);
    this.router.navigate(['/auth/login']);
  }

  getToken(): string | null {
    const token = localStorage.getItem(this.TOKEN_KEY);
    if (!token || token === 'undefined' || token === 'null') {
      return null;
    }
    return token;
  }

  isTokenExpired(): boolean {
    const expiresAt = localStorage.getItem(this.EXPIRES_KEY);
    if (!expiresAt) return true;
    return new Date() >= new Date(expiresAt);
  }

  private storeSession(response: AuthResponse): void {
    localStorage.setItem(this.TOKEN_KEY,   response.accessToken);
    localStorage.setItem(this.USER_KEY,    JSON.stringify(response.userInfo));
    localStorage.setItem(this.EXPIRES_KEY, response.expiresAt);
    this._currentUser.set(response.userInfo);
  }

  private loadStoredUser(): User | null {
    try {
      // If token is missing/invalid/expired, clear the whole session so the
      // navbar guard (`isLoggedIn`) doesn't render UI for a dead session.
      if (!this.getToken() || this.isTokenExpired()) {
        localStorage.removeItem(this.TOKEN_KEY);
        localStorage.removeItem(this.USER_KEY);
        localStorage.removeItem(this.EXPIRES_KEY);
        return null;
      }
      const raw = localStorage.getItem(this.USER_KEY);
      return raw ? (JSON.parse(raw) as User) : null;
    } catch {
      return null;
    }
  }
}
