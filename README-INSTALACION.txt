ITQS SOC SQL KPI - Instalación

1. Copia las carpetas del ZIP al proyecto local real:

C:\Users\WilliamAdrianCambron\OneDrive - IT Quest Solutions (ITQS)\Escritorio\PROYECTOS\ITQS.SupportOperationsCenter\ITQS.SupportOperationsCenter

2. Instala Dapper y Microsoft.Data.SqlClient:

dotnet add package Dapper
dotnet add package Microsoft.Data.SqlClient

3. Modifica Program.cs:

Agrega:
using ITQS.SupportOperationsCenter.Extensions;

Agrega después de los servicios de Blazor:
builder.Services.AddItqsSocServices();

4. Configura conexión local en appsettings.Development.json:

"ConnectionStrings": {
  "ReportesDB": "Server=TU_SERVIDOR_SQL;Database=REPORTES;User Id=TU_USUARIO;Password=TU_PASSWORD;TrustServerCertificate=True;"
}

5. En Azure App Service:

Configuration > Connection strings
Name: ReportesDB
Value: cadena de conexión real
Type: SQLServer

6. Copia al repo GitHub:

Copy-Item `
"C:\Users\WilliamAdrianCambron\OneDrive - IT Quest Solutions (ITQS)\Escritorio\PROYECTOS\ITQS.SupportOperationsCenter\ITQS.SupportOperationsCenter\Data" `
"C:\Users\WilliamAdrianCambron\OneDrive - IT Quest Solutions (ITQS)\Documentos\GitHub\PROYECTOS\" `
-Recurse -Force

Copy-Item `
"C:\Users\WilliamAdrianCambron\OneDrive - IT Quest Solutions (ITQS)\Escritorio\PROYECTOS\ITQS.SupportOperationsCenter\ITQS.SupportOperationsCenter\Models" `
"C:\Users\WilliamAdrianCambron\OneDrive - IT Quest Solutions (ITQS)\Documentos\GitHub\PROYECTOS\" `
-Recurse -Force

Copy-Item `
"C:\Users\WilliamAdrianCambron\OneDrive - IT Quest Solutions (ITQS)\Escritorio\PROYECTOS\ITQS.SupportOperationsCenter\ITQS.SupportOperationsCenter\Repositories" `
"C:\Users\WilliamAdrianCambron\OneDrive - IT Quest Solutions (ITQS)\Documentos\GitHub\PROYECTOS\" `
-Recurse -Force

Copy-Item `
"C:\Users\WilliamAdrianCambron\OneDrive - IT Quest Solutions (ITQS)\Escritorio\PROYECTOS\ITQS.SupportOperationsCenter\ITQS.SupportOperationsCenter\Services" `
"C:\Users\WilliamAdrianCambron\OneDrive - IT Quest Solutions (ITQS)\Documentos\GitHub\PROYECTOS\" `
-Recurse -Force

Copy-Item `
"C:\Users\WilliamAdrianCambron\OneDrive - IT Quest Solutions (ITQS)\Escritorio\PROYECTOS\ITQS.SupportOperationsCenter\ITQS.SupportOperationsCenter\Extensions" `
"C:\Users\WilliamAdrianCambron\OneDrive - IT Quest Solutions (ITQS)\Documentos\GitHub\PROYECTOS\" `
-Recurse -Force

Copy-Item `
"C:\Users\WilliamAdrianCambron\OneDrive - IT Quest Solutions (ITQS)\Escritorio\PROYECTOS\ITQS.SupportOperationsCenter\ITQS.SupportOperationsCenter\Components\Dashboard" `
"C:\Users\WilliamAdrianCambron\OneDrive - IT Quest Solutions (ITQS)\Documentos\GitHub\PROYECTOS\Components\" `
-Recurse -Force

Copy-Item `
"C:\Users\WilliamAdrianCambron\OneDrive - IT Quest Solutions (ITQS)\Escritorio\PROYECTOS\ITQS.SupportOperationsCenter\ITQS.SupportOperationsCenter\Components\Shared" `
"C:\Users\WilliamAdrianCambron\OneDrive - IT Quest Solutions (ITQS)\Documentos\GitHub\PROYECTOS\Components\" `
-Recurse -Force

Copy-Item `
"C:\Users\WilliamAdrianCambron\OneDrive - IT Quest Solutions (ITQS)\Escritorio\PROYECTOS\ITQS.SupportOperationsCenter\ITQS.SupportOperationsCenter\Components\Pages\SupportCloud.razor" `
"C:\Users\WilliamAdrianCambron\OneDrive - IT Quest Solutions (ITQS)\Documentos\GitHub\PROYECTOS\Components\Pages\" `
-Force

7. Build y push:

cd "C:\Users\WilliamAdrianCambron\OneDrive - IT Quest Solutions (ITQS)\Documentos\GitHub\PROYECTOS"

dotnet build
git add Data Models Repositories Services Extensions Components/Dashboard Components/Shared Components/Pages/SupportCloud.razor Program.cs
git commit -m "Add SQL KPI dashboard architecture"
git push origin main
