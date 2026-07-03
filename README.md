# CustomerAdmin - TenantId editable + eliminación lógica

## Cambios incluidos

- TenantId editable en el popup de Editar Cliente.
- Botón 🗑️ Eliminar en acciones.
- Popup de confirmación antes de eliminar.
- Eliminación lógica: `IsActive = 0`.
- Se agrega nota de auditoría con usuario y fecha UTC.
- Service y Repository actualizados.

## Archivos para reemplazar

Copiar estos archivos en sus rutas correspondientes:

```text
Components/Administration/Customers/CustomerAdmin.razor
Components/Administration/Customers/CustomerAdmin.razor.cs
Components/Administration/Customers/CustomerAdmin.razor.css
Services/Interfaces/ICustomerAdminService.cs
Services/CustomerAdminService.cs
Repositories/Interfaces/ICustomerAdminRepository.cs
Repositories/CustomerAdminRepository.cs
```

## SQL requerido

Ejecutar antes de probar edición de TenantId:

```text
SQL/01_Update_CustomerAdmin_SaveCustomer.sql
```

## Compilar

```powershell
dotnet build
```
