# ITQS Support Operations Center - Login Entra ID

Proyecto Blazor Web App .NET 8 con pantalla inicial limpia y autenticación Microsoft Entra ID.

## Rutas

- `/` Pantalla de bienvenida pública con botón Ingresar.
- `/home` Página protegida, solo visible después de autenticarse.

## Configuración requerida en Entra ID

Crear App Registration:

- Platform: Web
- Redirect URI local: `https://localhost:5001/signin-oidc`
- Redirect URI Azure: `https://TU-APP.azurewebsites.net/signin-oidc`
- Front-channel logout URL: `https://TU-APP.azurewebsites.net/signout-oidc`

Luego configurar `appsettings.json` o variables de entorno en Azure App Service:

- `AzureAd:TenantId`
- `AzureAd:ClientId`
- `AzureAd:Domain`

## Publicación

```powershell
dotnet build
git add .
git commit -m "Login Entra ID only"
git push origin main
```
