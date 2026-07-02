# ConnectHub

ConnectHub is a full-stack social network built with ASP.NET Core 8, Angular 18 and SQL Server. It covers the flow of a real social app: authentication, posts with images, likes, nested comments, follows, a personalized feed, real-time notifications, an analytics dashboard, hashtags and search.

## Features

- JWT authentication (register and login) with BCrypt password hashing.
- Posts with text and optional image, owner-only deletion.
- Likes with per-user state and live counters (optimistic UI).
- Nested comments (one reply level) with counters and thread deletion.
- Follows with public user profiles and follower and following counts.
- Personalized feed: global timeline or only the people you follow.
- Image uploads for posts and avatars, plus editable bio.
- Real-time notifications over SignalR for likes, comments and follows, with an unread badge, dropdown and live toast.
- Analytics dashboard: posts per day, likes received, follower growth, top posts and engagement rate, rendered with charts.
- Hashtags parsed automatically from post content, and search across posts, hashtags and users.
- Pagination with infinite scroll on the feed.
- Light and dark theme with persistence.

## Tech stack

| Layer | Technology |
|-------|------------|
| Frontend | Angular 18 (standalone components, signals, new control flow), TypeScript, ng2-charts, @microsoft/signalr |
| Backend | ASP.NET Core Web API (.NET 8), Entity Framework Core 8 (Code First) |
| Database | SQL Server |
| Auth | JWT Bearer, BCrypt |
| Real-time | SignalR |
| Tests | xUnit (backend), Jasmine/Karma (frontend) |
| CI/CD | GitHub Actions |

## Architecture

```
backend/ConnectHub.API/
  Controllers/   HTTP endpoints, no business logic
  DTOs/          request and response contracts (entities are never exposed)
  Models/        EF Core entities
  Data/          ApplicationDbContext and Fluent API configuration
  Services/      business logic (file storage, notifications)
  Hubs/          SignalR hubs
  Helpers/       utilities (JWT, hashtag parsing)
  Migrations/    EF Core migrations

frontend/connecthub-web/src/app/
  core/          singletons: services, guards, interceptors, models
  features/      feature areas: auth, feed, profile, dashboard, search
  shared/        reusable components (post card, notifications bell)
```

The backend follows a layered approach: controllers stay thin and delegate to services and the DbContext, and every response is projected into a DTO so EF entities are never returned directly. The frontend uses standalone components with signals for local state and functional guards and interceptors.

## Data model

Users, Posts, Likes, Comments (self-referencing for replies), Follows (composite key), Notifications, Hashtags and the PostHashtags join table.

## API overview

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/api/auth/register` | no | Create account, returns JWT |
| POST | `/api/auth/login` | no | Log in, returns JWT |
| GET | `/api/posts` | no | Paginated global feed |
| GET | `/api/posts/feed` | yes | Posts from people you follow |
| POST | `/api/posts` | yes | Create a post |
| DELETE | `/api/posts/{id}` | yes | Delete own post |
| POST | `/api/posts/upload-image` | yes | Upload a post image |
| POST/DELETE | `/api/posts/{id}/like` | yes | Toggle like |
| GET/POST | `/api/posts/{id}/comments` | mixed | List or add comments |
| DELETE | `/api/comments/{id}` | yes | Delete own comment |
| GET | `/api/users/{id}` | no | Public profile |
| POST/DELETE | `/api/users/{id}/follow` | yes | Follow or unfollow |
| POST | `/api/users/me/avatar` | yes | Upload avatar |
| PUT | `/api/users/me` | yes | Update bio |
| GET | `/api/notifications` | yes | My notifications |
| GET | `/api/me/stats/*` | yes | Dashboard statistics |
| GET | `/api/search?q=` | no | Search posts and users |

Authentication uses `Authorization: Bearer <jwt>`. The SignalR hub is served at `/hubs/notifications`.

## Getting started

Prerequisites: .NET 8 SDK, Node.js 20+, SQL Server (Express is fine). See [SETUP.md](./SETUP.md) for the detailed walkthrough.

```bash
# Backend
cd backend/ConnectHub.API
dotnet ef database update
dotnet run
# API on https://localhost:7088  (Swagger at /swagger)

# Frontend (new terminal)
cd frontend/connecthub-web
npm install
npm start
# App on http://localhost:4200
```

## Tests

```bash
# Backend
dotnet test backend/ConnectHub.Tests/ConnectHub.Tests.csproj

# Frontend
cd frontend/connecthub-web
npm test
```

## Continuous integration and deployment

`.github/workflows/ci.yml` builds and tests both the backend and the frontend on every push and pull request to `main`.

`.github/workflows/deploy.yml` deploys the API to Azure App Service and the web app to Azure Static Web Apps. It runs manually and expects two secrets, `AZURE_WEBAPP_PUBLISH_PROFILE` and `AZURE_STATIC_WEB_APPS_API_TOKEN`, plus the App Service name set in the workflow.
