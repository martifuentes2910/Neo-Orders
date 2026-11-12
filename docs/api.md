# API

Base URL: `http://localhost:5286/api`. Todas las rutas (excepto login) requieren autenticación por cookie.

## Auth
- `POST /auth/login` — iniciar sesión.
- `POST /auth/logout` — cerrar sesión.
- `GET /auth/me` — usuario actual.

## Clients
- `GET /clients?search=`
- `GET /clients/{id}`
- `POST /clients` (admin)
- `PUT /clients/{id}` (admin)
- `DELETE /clients/{id}` (admin)

## Orders
- `GET /orders?search=&estado=&clientId=&sellerId=`
- `GET /orders/{id}`
- `POST /orders` — crea en estado Pendiente.
- `PUT /orders/{id}`
- `DELETE /orders/{id}` (admin)
- `GET /orders/statuses`

## Products
- `GET /products?search=&category=&onlyInStock=`
- `GET /products/categories`
- `GET /products/{id}`
- `POST /products` (admin)
- `PUT /products/{id}` (admin)
- `DELETE /products/{id}` (admin)

## Sellers
- `GET /sellers?onlyActive=`
- `GET /sellers/{id}`
- `POST /sellers` (admin)
- `PUT /sellers/{id}` (admin)
- `DELETE /sellers/{id}` (admin)

## Dashboard y reportes
- `GET /dashboard/metrics`
- `GET /reports?desde=&hasta=&clientId=&sellerId=&estado=`

## Configuración
- `GET/PUT /config/company` (admin)
- `GET/PUT /config/profile`
- `POST /config/seed` (admin)

## Usuarios (admin)
- `GET /users`
- `POST /users`
- `PUT /users/{id}/rol`
- `DELETE /users/{id}`
