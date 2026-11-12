# Arquitectura

## Backend (ASP.NET Core 10)

- **Web API** con controladores organizados por dominio: `Auth`, `Pedidos`, `Clientes`, `Dashboard`, `Reportes`, `Configuracion`, `Usuarios`.
- **Entity Framework Core 10** con SQL Server. `AppDbContext` extiende `IdentityDbContext<ApplicationUser>`, integrando usuarios y roles con las entidades de negocio.
- **ASP.NET Core Identity** maneja autenticación por cookie HttpOnly y roles (Admin / Empleado). Las mutaciones validan token antiforgery (`X-XSRF-TOKEN`).
- **Seed** (`DbSeeder`) crea roles, usuarios, configuración de empresa, clientes y pedidos de ejemplo al iniciar (idempotente).

### Entidades principales

- `ApplicationUser`: datos de perfil y tema.
- `Cliente`: datos de contacto y relación con pedidos.
- `Order`: pedido vinculado a un cliente, con estado en español.
- `CompanySettings`: configuración global (nombre, moneda, tipo de cambio).

## Frontend (React 19 + Vite)

- **React Router** con rutas protegidas y layout con sidebar.
- **Contextos**: `AuthContext`, `ThemeContext`, `SettingsContext`.
- **Cliente HTTP** (`api.ts`) con credenciales y token antiforgery.
- **Recharts** para gráficos, **lucide-react** para iconos.

### Temas

Variables CSS en `src/styles/theme.css` aplicadas mediante `data-theme` en `<html>`. Claro y oscuro (gris negro en oscuro). Acentos: violeta, fucsia y turquesa.

## Flujo de autenticación

1. Login (`POST /api/auth/login`) establece cookie de sesión.
2. `GET /api/auth/me` recupera el usuario actual.
3. Las mutaciones envían `X-XSRF-TOKEN` obtenido de `/api/antiforgery/token`.
4. Logout (`POST /api/auth/logout`) cierra la sesión.
