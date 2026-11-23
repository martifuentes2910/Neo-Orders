# Plan de despliegue: Azure + GitHub (alineado con KAN-32)

> Guardado el 2026-11-23. Rama de trabajo: `refactor/kan-32-s0`. No push sin autorización explícita.

## Objetivo

Preparar NeoOrders para despliegue en Azure usando GitHub Actions, con GitHub Student, sin romper contratos ni el refactor KAN-32 en curso.

## Arquitectura objetivo

| Capa | Servicio | Motivo |
|---|---|---|
| Frontend React/Vite | Azure Static Web Apps (Free) | SPA estática, HTTPS, CI/CD con GitHub. |
| Backend ASP.NET Core 10 | Azure App Service Linux | API + Identity cookies + EF Core. Confirmar disponibilidad runtime .NET 10 en región; fallback: contenedor. |
| Base de datos | Azure SQL Database (SKU baja/serverless según crédito) | Compatible con SQL Server/EF Core/migraciones existentes. |
| Imágenes | Cloudinary (existente) | Ya integrado; no agregar Blob Storage ahora. |

## Alineación con KAN-32

- **Compatible**: config por entorno, CORS configurable, workflows CI/CD, smoke/gates (KAN-39/KAN-40/KAN-44).
- **No mezclar**: KAN-38 profundo (Domain puro vs EF/Identity/migraciones). Hacer deploy-prep como slice aislado.
- **Restricciones**: preservar contratos HTTP, `MigrationsAssembly`, migraciones, shapes CSV/error; no push sin autorización; commits grandes.

## Bloques

### Bloque A — Deploy-prep seguro (en repo) [EN PROGRESO]
- [ ] CORS configurable por `Frontend:Origins` / env vars, con localhost por defecto en dev.
- [ ] Seed de startup configurable (`Seed:RunOnStartup`, default `true` en dev / configurable en prod). Evitar credenciales demo automáticas en prod por defecto.
- [ ] Exponer variables requeridas en `README.md`.
- [ ] Validar `dotnet publish`, `dotnet build/test`, `npm run build`.

### Bloque B — CI/CD GitHub Actions (en repo) [EN PROGRESO]
- [ ] Mantener `ci.yml` existente.
- [ ] Agregar `deploy-api.yml` (Azure App Service; publish profile u OIDC; `workflow_dispatch` inicial).
- [ ] Agregar `deploy-frontend.yml` (Azure Static Web Apps; `VITE_API_URL` por secret; `workflow_dispatch` inicial).
- [ ] Deploy automático a `main` solo tras decisión de merge.

### Bloque C — Provisionar Azure (manual/fuera de código)
- [ ] Resource group `rg-neoorders-dev`.
- [ ] Azure SQL Server + Database (SKU baja/serverless, auto-pause si disponible).
- [ ] App Service Plan Linux + Web App .NET 10.
- [ ] Static Web App.
- [ ] Configurar App Settings: `ConnectionStrings__DefaultConnection`, `Cloudinary__*`, `Frontend__Origins__0`, `Seed__RunOnStartup=false`, `ASPNETCORE_ENVIRONMENT=Production`.
- [ ] Configurar GitHub secrets: `AZURE_WEBAPP_PUBLISH_PROFILE` o OIDC, `AZURE_STATIC_WEB_APPS_API_TOKEN`, `VITE_API_URL`.

### Bloque D — Smoke + cierre KAN-40
- [ ] Smoke en Azure: login, CRUD, CSV, imágenes, reportes, roles.
- [ ] Validar cookies/CSRF/CORS cross-site entre SWA y App Service.
- [ ] Gates finales CI/remoto.

## Riesgos

- `DbSeeder` crea usuarios demo con contraseñas conocidas; no ejecutar automáticamente en prod.
- CORS/cookies cross-site: SWA y App Service en dominios distintos; probar CSRF.
- Azure SQL puede consumir crédito; elegir SKU mínima/serverless.
- Runtime .NET 10 en App Service depende de región/disponibilidad.
- `docs/` está ignorado en git; documentación de deploy va en README o archivos no ignorados.
