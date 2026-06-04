import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-container">
      <h2>Crear cuenta</h2>

      <form [formGroup]="form" (ngSubmit)="onSubmit()">
        <label>
          Username
          <input type="text" formControlName="username" />
        </label>

        <label>
          Email
          <input type="email" formControlName="email" />
        </label>

        <label>
          Contraseña
          <input type="password" formControlName="password" />
        </label>

        @if (error()) {
          <p class="error">{{ error() }}</p>
        }

        <button type="submit" [disabled]="form.invalid || loading()">
          {{ loading() ? 'Creando...' : 'Crear cuenta' }}
        </button>
      </form>

      <p>¿Ya tienes cuenta? <a routerLink="/login">Inicia sesión</a></p>
    </div>
  `,
  styles: [`
    .auth-container { max-width: 400px; margin: 4rem auto; padding: 2rem; }
    form { display: flex; flex-direction: column; gap: 1rem; }
    label { display: flex; flex-direction: column; gap: 0.25rem; }
    input { padding: 0.5rem; }
    button { padding: 0.75rem; cursor: pointer; }
    .error { color: red; }
  `]
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);

  loading = signal(false);
  error = signal<string | null>(null);

  form = this.fb.nonNullable.group({
    username: ['', [Validators.required, Validators.minLength(3)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  onSubmit() {
    if (this.form.invalid) return;

    this.loading.set(true);
    this.error.set(null);

    this.auth.register(this.form.getRawValue()).subscribe({
      next: () => this.router.navigate(['/feed']),
      error: (err) => {
        this.error.set(err?.error?.message ?? 'Error al registrar');
        this.loading.set(false);
      }
    });
  }
}
