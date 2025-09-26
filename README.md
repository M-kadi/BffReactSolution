# BffReactSolution

This repository contains a **full-stack example** demonstrating different approaches to building a secure API with JWT and integrating it with a React UI.

---

## 📂 Projects in this Solution

### 1. **BffServer**
- Backend-for-Frontend (BFF) API.
- Provides secure API endpoints for the React UI.
- Implements **JWT authentication**, but **CSRF protection is disabled** in this project.
- Handles:
  - Login / Logout via JWT.
  - Proxying API requests through the BFF.
  - Session management.

👉 Use this project to learn about the **BFF pattern without CSRF**.

---

### 2. **JwtApi**
- Traditional API secured with JWT (no BFF).
- The React UI or any client app communicates directly with this API by attaching JWT tokens in the `Authorization` header.
- Provides:
  - User authentication (`/api/auth/login`).
  - Student and Teacher endpoints (`/api/students`, `/api/teachers`).

👉 This is the **classic MVC/Web API style** JWT-secured backend.

---

### 3. **ReactUI_BFF**
- React frontend built with **Vite + React**.
- Communicates with the **BffServer** using `fetch` with credentials and CSRF headers (if enabled).
- Contains UI pages for:
  - Login/Logout
  - Student management (CRUD)
  - Teacher management
- Configured with HTTPS and proxy to `BffServer` in `vite.config.js`.

👉 Use this project to test **frontend-to-BFF integration**.

---

## 🔑 Security Models Compared

1. **JwtApi (Old API)**  
   - JWT stored on the client.  
   - React UI talks **directly** to the API.  
   - Common, but risk of token leakage (e.g., XSS).

2. **BffServer API (without CSRF)**  
   - JWT stored in **server session** (not in the browser).  
   - React UI talks only to the **BFF**.  
   - Safer than direct API access.  

3. **BffServer API + CSRF (future variant)**  
   - Adds **CSRF protection** to BFF routes.  
   - Requires CSRF token handling in the React UI.  
   - Most secure model (but more complex).

---

## 🚀 Getting Started

### Backend
1. Open solution in **Visual Studio 2022**.
2. Run **JwtApi** Project
   https://localhost:54451/
4. Run **BffServer** Project
   https://localhost:5099

### React UI
1. Go to `ReactUI_BFF` folder:
   ```bash
   cd ReactUI_BFF
   npm install
   npm run dev
      http://localhost:54453/
