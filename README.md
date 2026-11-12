# NeoOrders — Sistema de gestión de pedidos

Aplicación full-stack para la gestión de pedidos, clientes, productos, vendedores y reportes.

- **Backend**: ASP.NET Core 10 (Web API) + Entity Framework Core 10 + SQL Server + ASP.NET Core Identity
- **Frontend**: React 19 + Vite + TypeScript + Recharts

## Requisitos

- .NET 10 SDK
- Node.js 20+
- SQL Server (instancia local, autenticación de Windows)

## Ejecutar el backend

```bash
cd backend/OrdersApi
dotnet run
```

La API queda disponible en `http://localhost:5286`. La base de datos `OrdersDb` se crea y se siembran datos de ejemplo automáticamente al iniciar.

Documentación OpenAPI: `http://localhost:5286/openapi/v1.json`.

## Ejecutar el frontend

```bash
cd frontend/client
npm install
npm run dev
```

El frontend queda disponible en `http://localhost:5173`.

## Cuentas de ejemplo

| Rol | Email | Contraseña |
|-----|-------|------------|
| Administrador | admin@neoorders.com | Admin123! |
| Empleado | empleado@neoorders.com | Empleado123! |

## Estructura del proyecto

```
backend/OrdersApi   -> API ASP.NET Core, Identity, EF Core, servicios
frontend/client     -> Aplicación React (Vite)
docs/               -> Documentación técnica y de usuario
```

## Funcionalidades

- Panel de control con métricas y gráficos.
- Gestión de pedidos (CRUD, filtros, estados en español, nuevos pedidos inician en Pendiente).
- Gestión de clientes (CRUD, historial y total gastado).
- Catálogo de productos (CRUD, búsqueda, filtro por categoría, alerta de stock crítico).
- Gestión de vendedores (CRUD, comisiones).
- Reportes con filtros (período, cliente, vendedor, estado) y 5 gráficos, incluyendo ventas por vendedor.
- Configuración: perfil de usuario, empresa, moneda (ARS/USD con tipo de cambio), usuarios (admin) y datos de ejemplo.
- Tema claro/oscuro con acentos coral, durazno y ámbar.
- Tipografía Plus Jakarta Sans.
- Autenticación con roles (Administrador / Empleado).

## Documentación adicional

Ver la carpeta `docs/`.
