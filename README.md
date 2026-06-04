# ConnectHub 🌐

Mini red social tipo Twitter/Instagram construida con **.NET 8 + Angular 18 + SQL Server**.

> Proyecto de aprendizaje full-stack desarrollado por fases, cada una añadiendo capacidades del stack.

## ✨ Features (al terminar todas las fases)

- 🔐 Autenticación JWT con BCrypt
- 📝 Posts con texto e imágenes
- ❤️ Likes y comentarios anidados
- 👥 Sistema de seguidores y feed personalizado
- 📷 Subida de imágenes
- 🔔 Notificaciones en tiempo real (SignalR)
- 💬 Mensajería directa
- 📊 Dashboard con estadísticas (Chart.js)
- 🏷️ Hashtags y búsqueda

## 🛠️ Stack

| Capa | Tech |
|------|------|
| Frontend | Angular 18 (standalone, signals), TypeScript, Angular Material |
| Backend | ASP.NET Core Web API (.NET 8), EF Core (Code First) |
| BD | SQL Server Express |
| Auth | JWT Bearer |
| Tiempo real | SignalR |

## 🚀 Quick Start

Sigue la guía paso a paso: **[SETUP.md](./SETUP.md)**

```bash
# Backend
cd backend/ConnectHub.API
dotnet ef database update
dotnet run

# Frontend (nueva terminal)
cd frontend/connecthub-web
ng serve
```

Backend: `https://localhost:7XXX/swagger`
Frontend: `http://localhost:4200`

## 📅 Roadmap

- [x] **Fase 1** — Fundamentos: auth JWT + CRUD posts
- [ ] **Fase 2** — Interacciones: likes, comentarios, seguidores
- [ ] **Fase 3** — Subida de archivos / imágenes
- [ ] **Fase 4** — Tiempo real con SignalR (notificaciones, chat)
- [ ] **Fase 5** — Dashboard con gráficos y estadísticas
- [ ] **Fase 6** — Búsqueda, hashtags, paginación infinita, deploy
