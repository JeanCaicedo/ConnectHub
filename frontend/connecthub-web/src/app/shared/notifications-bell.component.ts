import { Component, OnDestroy, OnInit, effect, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { NotificationService } from '../core/services/notification.service';
import { Notification } from '../core/models/models';

@Component({
  selector: 'app-notifications-bell',
  standalone: true,
  template: `
    <div class="bell-wrap">
      <button class="bell" (click)="toggle()" aria-label="Notificaciones">
        🔔
        @if (notif.unreadCount() > 0) {
          <span class="badge">{{ notif.unreadCount() }}</span>
        }
      </button>

      @if (open()) {
        <div class="dropdown">
          <div class="dropdown-head">
            <strong>Notificaciones</strong>
            <button class="link" (click)="notif.markAllRead()">Marcar todas leídas</button>
          </div>
          @for (n of notif.notifications(); track n.id) {
            <div class="item" [class.unread]="!n.isRead" (click)="onClick(n)">
              {{ text(n) }}
            </div>
          } @empty {
            <div class="item muted">No tienes notificaciones.</div>
          }
        </div>
      }

      @if (toast(); as t) {
        <div class="toast">{{ text(t) }}</div>
      }
    </div>
  `,
  styles: [`
    .bell-wrap { position: relative; }
    .bell { position: relative; background: none; border: none; font-size: 1.3rem; cursor: pointer; }
    .badge { position: absolute; top: -4px; right: -6px; background: #c92a2a; color: #fff; border-radius: 10px; font-size: 0.7rem; padding: 0 5px; }
    .dropdown { position: absolute; right: 0; top: 2rem; width: 280px; background: var(--card, #fff); border: 1px solid var(--border, #ddd); border-radius: 8px; box-shadow: 0 4px 16px rgba(0,0,0,.12); z-index: 20; max-height: 360px; overflow-y: auto; }
    .dropdown-head { display: flex; justify-content: space-between; align-items: center; padding: 0.6rem 0.75rem; border-bottom: 1px solid var(--border, #eee); }
    .item { padding: 0.6rem 0.75rem; cursor: pointer; font-size: 0.9rem; border-bottom: 1px solid var(--border, #f2f2f2); }
    .item:hover { background: rgba(25,113,194,.08); }
    .item.unread { background: rgba(25,113,194,.12); font-weight: 600; }
    .item.muted { color: #888; cursor: default; }
    .link { background: none; border: none; color: #1971c2; cursor: pointer; font-size: 0.75rem; }
    .toast { position: fixed; bottom: 1.5rem; right: 1.5rem; background: #1971c2; color: #fff; padding: 0.75rem 1rem; border-radius: 8px; box-shadow: 0 4px 16px rgba(0,0,0,.2); z-index: 50; }
  `]
})
export class NotificationsBellComponent implements OnInit, OnDestroy {
  notif = inject(NotificationService);
  private router = inject(Router);

  open = signal(false);
  toast = signal<Notification | null>(null);

  constructor() {
    // effect(): reacciona cuando llega una notificación en vivo y muestra el toast.
    effect(() => {
      const latest = this.notif.latest();
      if (latest) {
        this.toast.set(latest);
        setTimeout(() => this.toast.set(null), 4000);
      }
    });
  }

  ngOnInit() {
    this.notif.load();
    this.notif.startConnection();
  }

  ngOnDestroy() {
    // No cerramos la conexión aquí: el servicio es singleton y otras vistas
    // (perfil, dashboard) también muestran la campana.
  }

  toggle() {
    this.open.update(v => !v);
  }

  text(n: Notification): string {
    const who = '@' + n.fromUsername;
    switch (n.type) {
      case 'Like': return `${who} le dio me gusta a tu publicación`;
      case 'Comment': return `${who} comentó tu publicación`;
      case 'Follow': return `${who} empezó a seguirte`;
      default: return `${who} interactuó contigo`;
    }
  }

  onClick(n: Notification) {
    if (!n.isRead) this.notif.markRead(n.id);
    this.open.set(false);
    // Sin página de detalle de post: Follow lleva al perfil, el resto al feed.
    if (n.type === 'Follow') {
      this.router.navigate(['/profile', n.fromUserId]);
    } else {
      this.router.navigate(['/feed']);
    }
  }
}
