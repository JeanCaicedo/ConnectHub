# SETUP — Levantar ConnectHub desde cero

Esta guía es para la **primera vez** que armas el proyecto. Sigue los pasos en orden.

---

## 📦 Pre-requisitos

```bash
dotnet --version    # 8.x.x
node --version      # 20+
ng version          # 18+

# Instala Angular CLI si no lo tienes
npm install -g @angular/cli

# Instala herramientas EF Core (una sola vez en tu PC)
dotnet tool install --global dotnet-ef
```

**SQL Server Express:** Descarga desde https://www.microsoft.com/sql-server/sql-server-downloads (versión Express, gratis). Instala también **SSMS** (SQL Server Management Studio) para inspeccionar la BD.

---

## 1️⃣ Backend

### a. Crear el proyecto

```bash
cd backend
dotnet new webapi -n ConnectHub.API -f net8.0 --use-controllers
cd ConnectHub.API
```

### b. Instalar paquetes NuGet

```bash
dotnet add package Microsoft.EntityFrameworkCore --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.0
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.0
dotnet add package BCrypt.Net-Next
```

### c. Copiar los archivos de este repo

Copia el contenido de `backend/` (en este paquete) dentro de tu `backend/ConnectHub.API/`:

```
backend/ConnectHub.API/
├── Controllers/         ← AuthController.cs, PostsController.cs
├── Models/              ← User.cs, Post.cs
├── DTOs/                ← Dtos.cs
├── Data/                ← ApplicationDbContext.cs
├── Helpers/             ← JwtHelper.cs
├── Program.cs           ← (reemplaza el generado por dotnet new)
└── appsettings.json     ← (reemplaza el generado)
```

### d. Configurar la cadena de conexión

Abre `appsettings.json` y ajusta `DefaultConnection`:

| Tipo de SQL Server | Cadena |
|---|---|
| SQL Server Express local | `Server=.\\SQLEXPRESS;Database=ConnectHubDb;Trusted_Connection=True;TrustServerCertificate=True` |
| LocalDB | `Server=(localdb)\\MSSQLLocalDB;Database=ConnectHubDb;Trusted_Connection=True` |
| SQL Server con user/pwd | `Server=localhost;Database=ConnectHubDb;User Id=sa;Password=TuPwd;TrustServerCertificate=True` |

### e. Crear la base de datos

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Verifica en SSMS que se creó `ConnectHubDb` con tablas `Users` y `Posts`.

### f. Correr

```bash
dotnet run
```

Mira el puerto HTTPS en la consola (algo como `https://localhost:7123`). Abre `https://localhost:7123/swagger` y prueba:

1. POST `/api/auth/register` → te devuelve un token
2. Click en **Authorize** arriba a la derecha, pega `Bearer <token>`
3. POST `/api/posts` con `{"content": "Mi primer post"}`
4. GET `/api/posts` para verlo en la lista

---

## 2️⃣ Frontend

### a. Crear el proyecto Angular

```bash
cd ../../frontend
ng new connecthub-web --standalone --routing --style=scss --ssr=false
cd connecthub-web
```

### b. Instalar dependencias

```bash
npm install @microsoft/signalr   # para Fase 4 (instálalo ya)
```

### c. Copiar los archivos de este repo

Reemplaza el contenido de `src/app/` con el de `frontend/src/app/` de este paquete:

```
src/app/
├── core/
│   ├── services/        ← auth.service.ts, post.service.ts
│   ├── guards/          ← auth.guard.ts
│   ├── interceptors/    ← auth.interceptor.ts
│   └── models/          ← models.ts
├── features/
│   ├── auth/
│   │   ├── login/       ← login.component.ts
│   │   └── register/    ← register.component.ts
│   └── feed/            ← feed.component.ts
├── app.component.ts     ← (reemplaza)
├── app.config.ts        ← (reemplaza)
└── app.routes.ts        ← (reemplaza)
```

### d. ⚠️ Ajustar el puerto del API

Edita los dos archivos y cambia el puerto al que te mostró `dotnet run`:

- `src/app/core/services/auth.service.ts` → línea con `apiUrl`
- `src/app/core/services/post.service.ts` → línea con `apiUrl`

### e. Correr

```bash
ng serve
```

Abre `http://localhost:4200`:

1. Te redirige a `/login` (el guard te protege `/feed`)
2. Click en "Regístrate"
3. Crea una cuenta → te lleva al feed
4. Escribe un post y publica
5. Recarga la página → el token persiste en `localStorage`

---

## ✅ Checklist final

- [ ] `dotnet run` corre sin errores y muestra Swagger
- [ ] Migración aplicada, BD `ConnectHubDb` existe en SSMS
- [ ] Registro y login funcionan en Swagger
- [ ] `ng serve` corre sin errores en `localhost:4200`
- [ ] Te puedes registrar/loguear desde el frontend
- [ ] Crear y eliminar posts funciona end-to-end
- [ ] El token se manda automáticamente (revisa Network tab del navegador: header `Authorization: Bearer ...`)

---

## 🆘 Troubleshooting

**"Cannot open server" al hacer `database update`**
→ Cadena de conexión mal. Verifica el nombre exacto del servidor en SSMS (esquina superior izquierda al conectar).

**"A network-related or instance-specific error"**
→ El servicio "SQL Server (SQLEXPRESS)" no está corriendo. Abre Services.msc en Windows e inícialo.

**CORS error en el navegador**
→ El puerto del frontend no coincide con `WithOrigins("http://localhost:4200")` en `Program.cs`. Si ng serve usa otro puerto, ajústalo.

**401 Unauthorized aunque mandes el token**
→ Verifica en DevTools → Network que el header se llame exactamente `Authorization` y empiece con `Bearer ` (con espacio).

**"No project was found" al correr `dotnet ef`**
→ Estás en la carpeta incorrecta. `cd` al directorio que contiene `ConnectHub.API.csproj`.

**El frontend no llama al backend**
→ Revisa que el puerto en los services Angular coincida con el HTTPS de `dotnet run`. Acepta el certificado autofirmado abriendo el URL del API directamente en el navegador la primera vez.
