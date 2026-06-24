# ITQS Support Operations Center

Blazor Web App .NET 8 para publicar en Azure App Service y conectar contra SQL Server `REPORTES` usando Dapper y `Microsoft.Data.SqlClient`.

## Cambio aplicado

La interfaz ahora queda organizada para crecimiento por áreas:

- **Home**: pantalla principal de bienvenida.
- **Soporte Cloud**:
  - **Gestor de Alertas**
    - Dashboard
    - Asignadas
- **Soporte 365**: reservado para una siguiente fase.

En el Dashboard del Gestor de Alertas se muestran únicamente KPIs operativos de alertas cloud:

- Total Alertas
- Asignadas a mí
- Sin Asignar
- Críticas
- Altas
- Medias / Bajas

Se removieron los cuadros separados de `Service Health Alerts` y `Security Alerts` de este módulo porque esos irán en módulos independientes/futuros.

## MVP incluido

- Sidebar oscuro estilo Azure Portal.
- Home con bienvenida y selección de área de soporte.
- Soporte Cloud > Gestor de Alertas.
- Dashboard con alertas sin asignar y botón **Asignar**.
- Vista **Asignadas** con alertas del usuario actual.
- Botones **Ver Detalle**, **In Progress** y **Cerrar**.
- Paginación máxima de 50 registros.
- Detalle e historial bajo demanda; no se cargan hasta presionar **Ver Detalle**.
- Stored procedures parametrizados.
- Sin `SELECT *`.

## Requisitos

- .NET SDK 8.x
- SQL Server con base `REPORTES`
- Tablas principales:
  - `dbo.AlertsManagement`
  - `dbo.AzureAlertCloseQueue`
  - `dbo.AlertUpdatesHistory`
- Vistas usadas:
  - `dbo.vw_AllAssignedAlerts_SoT_Norm_v3`
  - `dbo.vw_Dashboard_Unassigned_SoT_Norm_v2`

## Configurar conexión

Editar `appsettings.json`:

```json
"ConnectionStrings": {
  "ReportesDb": "Server=YOUR_SQL_SERVER;Database=REPORTES;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;Encrypt=True;"
}
```

Editar usuario operativo:

```json
"AppSettings": {
  "SupportUserName": "William Adrian Cambronero Acosta",
  "SupportUserEmail": "wcambronero@itqscr.com"
}
```

## Instalar stored procedures

Ejecutar contra la base `REPORTES`:

```sql
Sql/01_CreateStoredProcedures.sql
```

## Ejecutar local

```powershell
dotnet restore
dotnet build
dotnet run
```

Abrir:

```text
http://localhost:5000
```

## Publicar en Azure App Service

```powershell
dotnet publish -c Release
```

Luego publicar desde Visual Studio o Deployment Center hacia el App Service:

```text
ITQSSupportOperationsCenter
```

## Nota

Si las vistas no tienen la columna `Events`, ajuste los stored procedures para tomar `Events` desde `dbo.AlertsManagement` o quite esa columna del SELECT. El modelo ya está preparado para mostrar eventos cuando exista la columna.
