using ITQS.SupportOperationsCenter.Components;
using ITQS.SupportOperationsCenter.Data;
using ITQS.SupportOperationsCenter.Extensions;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using ITQS.SupportOperationsCenter.Repositories;
using ITQS.SupportOperationsCenter.Repositories.Interfaces;
using ITQS.SupportOperationsCenter.Services;
using ITQS.SupportOperationsCenter.Services.Interfaces;
using ITQS.SupportOperationsCenter.Models.Administration.Customers;
using ITQS.SupportOperationsCenter.Models.Administration.AppRegistrations;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;



var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

builder.Services.Configure<SqlSettings>(
    builder.Configuration.GetSection("SqlSettings"));

builder.Services.Configure<KeyVaultSettings>(
    builder.Configuration.GetSection("KeyVaultSettings"));
	
builder.Services.AddSingleton<SecretClient>(sp =>
{
    var keyVaultSettings = builder.Configuration
        .GetSection("KeyVaultSettings")
        .Get<KeyVaultSettings>();

    if (keyVaultSettings == null || string.IsNullOrWhiteSpace(keyVaultSettings.VaultName))
        throw new InvalidOperationException("KeyVaultSettings:VaultName no está configurado.");

    var vaultUri = new Uri($"https://{keyVaultSettings.VaultName}.vault.azure.net/");

    return new SecretClient(vaultUri, new DefaultAzureCredential());
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = null;
});

builder.Services
    .AddControllersWithViews(options =>
    {
        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();

        options.Filters.Add(new AuthorizeFilter(policy));
    })
    .AddMicrosoftIdentityUI();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddItqsSocServices(builder.Configuration);
builder.Services.AddScoped<IAdminManagerRepository, AdminManagerRepository>();
builder.Services.AddScoped<IAdminManagerService, AdminManagerService>();
builder.Services.AddScoped<IAlertMonitoringDashboardRepository, AlertMonitoringDashboardRepository>();
builder.Services.AddScoped<IAlertMonitoringDashboardService, AlertMonitoringDashboardService>();
builder.Services.AddScoped<ISqlMaintenanceRepository, SqlMaintenanceRepository>();
builder.Services.AddScoped<ISqlMaintenanceService, SqlMaintenanceService>();
builder.Services.AddScoped<ISqlOperationsRepository, SqlOperationsRepository>();
builder.Services.AddScoped<ISqlOperationsService, SqlOperationsService>();
builder.Services.AddScoped<ISqlOperationsDashboardRepository, SqlOperationsDashboardRepository>();
builder.Services.AddScoped<ISqlOperationsDashboardService, SqlOperationsDashboardService>();
builder.Services.AddScoped<ICustomerAdminRepository, CustomerAdminRepository>();
builder.Services.AddScoped<ICustomerAdminService, CustomerAdminService>();
builder.Services.Configure<AutomationRunbookSettings>(
builder.Configuration.GetSection("AutomationRunbook"));
builder.Services.AddHttpClient();
builder.Services.AddScoped<ICustomerConnectionRunbookService, CustomerConnectionRunbookService>();
builder.Services.Configure<AppRegistrationNotificationSettings>(
builder.Configuration.GetSection("AppRegistrationNotifications"));
builder.Services.AddScoped<IAppRegistrationITQSRepository, AppRegistrationITQSRepository>();
builder.Services.AddScoped<IAppRegistrationITQSService, AppRegistrationITQSService>();
builder.Services.AddScoped<IAppRegistrationNotificationService, AppRegistrationNotificationService>();


var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();