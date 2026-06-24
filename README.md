# ITQS Support Operations Center

Proyecto limpio Blazor Web App .NET 8 para ITQS SOC.

## Módulos incluidos

- Home
- Soporte Cloud
  - Gestor Alertas
  - Dashboard
  - Asignadas
  - Historial
  - Clientes
  - Inventory
  - Administración
- Soporte 365
- Seguridad

## Ejecutar local

```powershell
dotnet restore
dotnet build
dotnet run
```

## Publicar a Azure

```powershell
git add .
git commit -m "Clean SOC v2 project"
git push origin main
```

## Configuración SQL

Editar `appsettings.json` o configurar `ConnectionStrings:ReportesDb` en Azure App Service.

Ejecutar en SQL Server:

```sql
Sql/01_CreateStoredProcedures.sql
```
