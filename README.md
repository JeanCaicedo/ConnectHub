# ConnectHub

ConnectHub es una mini red social full-stack construida con ASP.NET Core 8, Angular 18 y SQL Server. Cubre el flujo de una app social real: autenticación, posts con imágenes, likes, comentarios anidados, follows, feed personalizado, notificaciones en tiempo real, dashboard de estadísticas, hashtags y búsqueda.

## Características

- Autenticación JWT (registro e inicio de sesión) con hashing de contraseñas mediante BCrypt.
- Posts con texto e imagen opcional, con borrado solo por el dueño.
- Likes con estado por usuario y contadores en vivo (UI optimista).
- Comentarios anidados (un nivel de respuesta) con contadores y borrado del hilo.
- Follows con perfiles públicos y conteo de seguidores y seguidos.
- Feed personalizado: timeline global o solo de las personas que sigues.
- Subida de imágenes para posts y avatares, además de bio editable.
- Notificaciones en tiempo real por SignalR para likes, comentarios y follows, con badge de no leídas, dropdown y toast en vivo.
- Dashboard de estadísticas: posts por día, likes recibidos, crecimiento de seguidores, top posts y tasa de engagement, renderizados con gráficos.
- Hashtags parseados automáticamente del contenido de los posts, y búsqueda sobre posts, hashtags y usuarios.
- Paginación con infinite scroll en el feed.
- Tema claro y oscuro con persistencia.

## Stack tecnológico

| Capa | Tecnología |
|-------|------------|
| Frontend | Angular 18 (standalone components, signals, nuevo control flow), TypeScript, ng2-charts, @microsoft/signalr |
| Backend | ASP.NET Core Web API (.NET 8), Entity Framework Core 8 (Code First) |
| Base de datos | SQL Server |
| Auth | JWT Bearer, BCrypt |
| Tiempo real | SignalR |
| Tests | xUnit (backend), Jasmine/Karma (frontend) |
| CI/CD | GitHub Actions |

## Arquitectura

```
backend/ConnectHub.API/
  Controllers/   endpoints HTTP, sin lógica de negocio
  DTOs/          contratos de request y response (las entidades nunca se exponen)
  Models/        entidades EF Core
  Data/          ApplicationDbContext y configuración Fluent API
  Services/      lógica de negocio (almacenamiento de archivos, notificaciones)
  Hubs/          hubs de SignalR
  Helpers/       utilidades (JWT, parseo de hashtags)
  Migrations/    migraciones de EF Core

frontend/connecthub-web/src/app/
  core/          singletons: services, guards, interceptors, models
  features/      áreas funcionales: auth, feed, profile, dashboard, search
  shared/        componentes reutilizables (post card, campana de notificaciones)
```

El backend sigue un enfoque por capas: los controllers se mantienen delgados y delegan en los services y el DbContext, y cada respuesta se proyecta a un DTO para no devolver nunca entidades EF directamente. El frontend usa standalone components con signals para el estado local, y guards e interceptors funcionales.

## Modelo de datos

Users, Posts, Likes, Comments (auto-referencial para las respuestas), Follows (clave compuesta), Notifications, Hashtags y la tabla intermedia PostHashtags.

## Resumen del API

| Método | Ruta | Auth | Descripción |
|--------|-------|------|-------------|
| POST | `/api/auth/register` | no | Crea cuenta, devuelve JWT |
| POST | `/api/auth/login` | no | Inicia sesión, devuelve JWT |
| GET | `/api/posts` | no | Feed global paginado |
| GET | `/api/posts/feed` | sí | Posts de las personas que sigues |
| POST | `/api/posts` | sí | Crea un post |
| DELETE | `/api/posts/{id}` | sí | Borra un post propio |
| POST | `/api/posts/upload-image` | sí | Sube una imagen de post |
| POST/DELETE | `/api/posts/{id}/like` | sí | Alterna el like |
| GET/POST | `/api/posts/{id}/comments` | mixto | Lista o agrega comentarios |
| DELETE | `/api/comments/{id}` | sí | Borra un comentario propio |
| GET | `/api/users/{id}` | no | Perfil público |
| POST/DELETE | `/api/users/{id}/follow` | sí | Sigue o deja de seguir |
| POST | `/api/users/me/avatar` | sí | Sube el avatar |
| PUT | `/api/users/me` | sí | Actualiza la bio |
| GET | `/api/notifications` | sí | Mis notificaciones |
| GET | `/api/me/stats/*` | sí | Estadísticas del dashboard |
| GET | `/api/search?q=` | no | Busca posts y usuarios |

La autenticación usa `Authorization: Bearer <jwt>`. El hub de SignalR se sirve en `/hubs/notifications`.

## Puesta en marcha

Requisitos previos: SDK de .NET 8, Node.js 20+, SQL Server (Express sirve). Ver [SETUP.md](./SETUP.md) para la guía detallada.

```bash
# Backend
cd backend/ConnectHub.API
dotnet ef database update
dotnet run
# API en https://localhost:7088  (Swagger en /swagger)

# Frontend (nueva terminal)
cd frontend/connecthub-web
npm install
npm start
# App en http://localhost:4200
```

## Tests

```bash
# Backend
dotnet test backend/ConnectHub.Tests/ConnectHub.Tests.csproj

# Frontend
cd frontend/connecthub-web
npm test
```

## Integración continua y despliegue

`.github/workflows/ci.yml` compila y testea backend y frontend en cada push y pull request a `main`.

`.github/workflows/deploy.yml` despliega el API en Azure App Service y la web en Azure Static Web Apps. Se ejecuta manualmente y espera dos secretos, `AZURE_WEBAPP_PUBLISH_PROFILE` y `AZURE_STATIC_WEB_APPS_API_TOKEN`, además del nombre del App Service definido en el workflow.
