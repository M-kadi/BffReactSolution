# React + .NET 9 BFF (No JWT in Browser)

This solution implements a **Backend-for-Frontend** pattern:
- The **BFF Server** (.NET 9) handles login against your **JwtApi** (`/api/auth/login`),
  stores the **JWT server-side in Session**, and proxies CRUD calls to **Students** and **Teachers**.
- The **React app** only calls the BFF endpoints (`/bff/*`). **No token is stored or visible in the browser.**

## Projects
- **BffServer** (ASP.NET Core .NET 9)
  - Endpoints:
    - `POST /bff/login` → calls JwtApi `/api/auth/login`, stores token in session
    - `POST /bff/logout` → clears session
    - Proxy:
      - `GET/POST/PUT/DELETE /bff/students[/id]` → forwards to JwtApi `/api/students[/id]`
      - `GET/POST/PUT/DELETE /bff/teachers[/id]` → forwards to JwtApi `/api/teachers[/id]`
- **ReactUI_BFF** (Vite + React Router)
  - `/login` page hits `/bff/login`
  - `/students` and `/teachers` call `/bff/*` endpoints
  - Nav has **API Swagger** link pointing to JwtApi

## Default ports
- JwtApi (your existing API): `https://localhost:54451` (Swagger at `/swagger`)
- **BffServer**: `https://localhost:5099`
- **React dev**: `http://localhost:5173`

## Run (dev)
1. **Run your JwtApi** on `https://localhost:54451` (or update URLs below).
2. In `BffServer/appsettings.json`, set `"JwtApiBase": "https://localhost:54451"` if needed.
3. Start **BffServer**:
   ```bash
   dotnet run --project BffServer
   ```
4. In `ReactUI_BFF`:
   ```bash
   npm install
   npm run dev
   ```
   Open http://localhost:5173

> The React app proxies `/bff/*` to the BFF at `https://localhost:5099` during development.

## Production
- Build React: `npm run build` (serves under `dist/`)
- Option A: Have **BffServer** serve the built static files under `/` (add StaticFiles + map to dist).
- Option B: Host React separately (Nginx/IIS) and keep BFF at `/bff` path.

Generated on: 2025-09-20T11:44:56.559701Z
