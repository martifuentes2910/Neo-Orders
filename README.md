# NeoOrders — Sistema de gestión de pedidos

Aplicación full-stack para la gestión de pedidos, clientes, productos, vendedores y reportes.

- **Backend**: ASP.NET Core 10 (Web API) + Entity Framework Core 10 + SQL Server + ASP.NET Core Identity
- **Frontend**: React 19 + Vite + TypeScript + Recharts

## Requisitos

- .NET 10 SDK
- Node.js 20+
- SQL Server (instancia local o Azure SQL)

## Ejecutar el backend

```bash
cd backend/src/NeoOrders.Api
dotnet run
```

La API queda disponible en `http://localhost:5286` (según perfil/launchSettings). La base de datos se migra al iniciar. El seed de datos de ejemplo se ejecuta en startup cuando `Seed:RunOnStartup=true` (por defecto en desarrollo).

Documentación OpenAPI (dev): `http://localhost:5286/openapi/v1.json`.

## Ejecutar el frontend

```bash
cd frontend/client
npm install
npm run dev
```

El frontend queda disponible en `http://localhost:5173`.

## Variables de configuración

### Backend (App Settings / env vars)

| Clave | Descripción | Ejemplo |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | Cadena SQL Server/Azure SQL | `Server=tcp:<server>.database.windows.net,1433;...` |
| `Frontend__Origins__0` | Origen permitido para CORS/cookies | `https://<app>.azurestaticapps.net` |
| `Frontend__Origins` | Alternativa CSV (env var) | `https://<app>.azurestaticapps.net,https://otro` |
| `Seed__RunOnStartup` | Ejecuta seed al iniciar | `false` en producción |
| `Cloudinary__CloudName` | Cloudinary cloud name | |
| `Cloudinary__ApiKey` | Cloudinary API key | |
| `Cloudinary__ApiSecret` | Cloudinary API secret | |
| `Cloudinary__Folder` | Carpeta Cloudinary | `neoorders/products` |
| `ASPNETCORE_ENVIRONMENT` | Entorno | `Production` |

### Frontend (build-time)

| Variable | Descripción | Ejemplo |
|---|---|---|
| `VITE_API_URL` | URL base de API, incluye `/api` | `https://<api>.azurewebsites.net/api` |

## Cuentas de ejemplo (solo entorno con seed demo)

| Rol | Email | Contraseña |
|-----|-------|------------|
| Administrador | admin@neoorders.com | Admin123! |
| Empleado | empleado@neoorders.com | Empleado123! |

> En producción, configurar `Seed__RunOnStartup=false` y crear usuarios reales por proceso seguro.

## Despliegue Azure + GitHub

Ver `AZURE_DEPLOY_PLAN.md` para el plan por bloques y checklist.

Resumen objetivo:
- Frontend: Azure Static Web Apps.
- Backend: Azure App Service Linux (.NET 10; validar runtime en región).
- DB: Azure SQL Database.
- Imágenes: Cloudinary.

Workflows:
- `.github/workflows/ci.yml`: CI build/test.
- `.github/workflows/deploy-api.yml`: deploy manual API a App Service.
- `.github/workflows/deploy-frontend.yml`: deploy manual frontend a Static Web Apps.

## Estructura del proyecto

```
backend/src/NeoOrders.Api            -> Host Web API
backend/src/NeoOrders.Application    -> Contratos/DTOs/casos de uso
backend/src/NeoOrders.Domain         -> Reglas de dominio
backend/src/NeoOrders.Infrastructure -> EF Core, Identity, servicios, migraciones
backend/tests                        -> Tests Domain/Application/Architecture/Infrastructure
frontend/client                      -> Aplicación React (Vite)
```

## Funcionalidades

- Panel de control con métricas y gráficos.
- Gestión de pedidos (CRUD, filtros, estados, importación CSV Admin-only).
- Gestión de clientes (CRUD, historial, importación CSV Admin-only).
- Catálogo de productos (CRUD, búsqueda, categoría, stock crítico, imagen Cloudinary, importación CSV Admin-only).
- Gestión de vendedores (CRUD, comisiones).
- Reportes con filtros y gráficos.
- Configuración: perfil, empresa, moneda ARS/USD, usuarios (admin), seed/clear.
- Tema claro/oscuro con paletas.
- Autenticación por cookies con roles (Administrador / Empleado) y antiforgery.
