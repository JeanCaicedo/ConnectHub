import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import { Notification } from '../models/models';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private http = inject(HttpClient);
  private auth = inject(AuthService);
  private apiUrl = `${environment.apiHost}/api/notifications`;
  private hubUrl = `${environment.apiHost}/hubs/notifications`;

  notifications = signal<Notification[]>([]);
  unreadCount = signal(0);
  // Última recibida en vivo, para mostrar un toast efímero.
  latest = signal<Notification | null>(null);

  private connection?: HubConnection;

  // Carga inicial desde la BD (las que ya existían).
  load() {
    this.http.get<Notification[]>(this.apiUrl).subscribe(data => {
      this.notifications.set(data);
      this.unreadCount.set(data.filter(n => !n.isRead).length);
    });
  }

  // Abre el WebSocket. Idempotente: si ya hay conexión, no crea otra.
  startConnection() {
    if (this.connection) return;

    this.connection = new HubConnectionBuilder()
      .withUrl(this.hubUrl, { accessTokenFactory: () => this.auth.token ?? '' })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    // El nombre "ReceiveNotification" debe coincidir con el del servidor.
    this.connection.on('ReceiveNotification', (n: Notification) => {
      this.notifications.update(list => [n, ...list]);
      this.unreadCount.update(c => c + 1);
      this.latest.set(n);
    });

    this.connection.start().catch(err => console.error('SignalR error:', err));
  }

  stopConnection() {
    this.connection?.stop();
    this.connection = undefined;
  }

  markRead(id: number) {
    this.http.post(`${this.apiUrl}/${id}/read`, {}).subscribe(() => {
      this.notifications.update(list =>
        list.map(n => (n.id === id ? { ...n, isRead: true } : n)));
      this.unreadCount.update(c => Math.max(0, c - 1));
    });
  }

  markAllRead() {
    this.http.post(`${this.apiUrl}/read-all`, {}).subscribe(() => {
      this.notifications.update(list => list.map(n => ({ ...n, isRead: true })));
      this.unreadCount.set(0);
    });
  }
}
