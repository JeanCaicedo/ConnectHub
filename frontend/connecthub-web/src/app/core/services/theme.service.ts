import { Injectable, signal } from '@angular/core';

type Theme = 'light' | 'dark';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  theme = signal<Theme>(this.load());

  constructor() {
    // Aplica el tema guardado al arrancar la app.
    this.apply(this.theme());
  }

  toggle() {
    const next: Theme = this.theme() === 'light' ? 'dark' : 'light';
    this.theme.set(next);
    this.apply(next);
    localStorage.setItem('connecthub_theme', next);
  }

  // El tema vive en una clase del <body>; las CSS variables hacen el resto.
  private apply(theme: Theme) {
    document.body.classList.toggle('dark', theme === 'dark');
  }

  private load(): Theme {
    return (localStorage.getItem('connecthub_theme') as Theme) ?? 'light';
  }
}
