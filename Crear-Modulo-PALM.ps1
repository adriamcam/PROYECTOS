$scriptFile = ".\Crear-Modulo-PALM.ps1"

@'
$ErrorActionPreference = "Stop"

$root = Get-Location

$moduleRoot = Join-Path $root "Components\Reporting\PALM"
$modelsPath = Join-Path $moduleRoot "Models"
$servicesPath = Join-Path $moduleRoot "Services"
$componentsPath = Join-Path $moduleRoot "Components"

New-Item -Path $moduleRoot -ItemType Directory -Force | Out-Null
New-Item -Path $modelsPath -ItemType Directory -Force | Out-Null
New-Item -Path $servicesPath -ItemType Directory -Force | Out-Null
New-Item -Path $componentsPath -ItemType Directory -Force | Out-Null

# ============================================================
# MODELOS
# ============================================================

@'
namespace ITQS.SupportOperationsCenter.Components.Reporting.PALM.Models;

public sealed class PalmDashboard
{
    public long TotalCustomers { get; set; }
    public long TotalOK { get; set; }
    public long TotalNOK { get; set; }
    public long TotalRefreshed { get; set; }
    public long PartnerIdCorrect { get; set; }
    public long WithoutPartnerId { get; set; }
    public long DifferentPartnerId { get; set; }
    public long RequiresAction { get; set; }
    public long TotalVisibleSubscriptions { get; set; }
    public decimal CompliancePercent { get; set; }
    public DateTime? FirstScanDate { get; set; }
    public DateTime? LastScanDate { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
}

public sealed class PalmResult
{
    public long Id { get; set; }
    public Guid? RunId { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public Guid TenantId { get; set; }

    public string? SubscriptionName { get; set; }
    public Guid? SubscriptionId { get; set; }

    public string Status { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;

    public string? PartnerIdBefore { get; set; }
    public string? PartnerIdCurrent { get; set; }
    public string? PartnerIdTarget { get; set; }

    public string PartnerValidationStatus { get; set; } = string.Empty;
    public bool RequiresAction { get; set; }

    public int? VisibleSubscriptions { get; set; }
    public string? ClientId { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Detail { get; set; }

    public DateTime ScanDate { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class PalmRun
{
    public Guid RunId { get; set; }

    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public int? DurationSeconds { get; set; }

    public int TotalCustomers { get; set; }
    public int TotalSubscriptions { get; set; }
    public int TotalOK { get; set; }
    public int TotalNOK { get; set; }

    public decimal SuccessPercent { get; set; }

    public string Status { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class PalmReportData
{
    public PalmDashboard Dashboard { get; set; } = new();
    public PalmRun? LatestRun { get; set; }

    public List<PalmResult> Results { get; set; } = [];
    public List<PalmResult> RequiresAction { get; set; } = [];
    public List<PalmRun> RunHistory { get; set; } = [];
}
'@ | Set-Content `
    -Path (Join-Path $modelsPath "PalmModels.cs") `
    -Encoding UTF8

# ============================================================
# INTERFAZ
# ============================================================

@'
using ITQS.SupportOperationsCenter.Components.Reporting.PALM.Models;

namespace ITQS.SupportOperationsCenter.Components.Reporting.PALM.Services;

public interface IPalmReportService
{
    Task<PalmReportData> GetReportAsync();
    Task<PalmDashboard> GetDashboardAsync();
    Task<List<PalmResult>> GetResultsAsync();
    Task<List<PalmResult>> GetRequiresActionAsync();
    Task<PalmRun?> GetLatestRunAsync();
    Task<List<PalmRun>> GetRunHistoryAsync();
}
'@ | Set-Content `
    -Path (Join-Path $servicesPath "IPalmReportService.cs") `
    -Encoding UTF8

# ============================================================
# SERVICIO DAPPER
# ============================================================

@'
using Dapper;
using Microsoft.Data.SqlClient;
using ITQS.SupportOperationsCenter.Components.Reporting.PALM.Models;

namespace ITQS.SupportOperationsCenter.Components.Reporting.PALM.Services;

public sealed class PalmReportService : IPalmReportService
{
    private readonly IConfiguration _configuration;

    public PalmReportService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private SqlConnection CreateConnection()
    {
        var connectionString =
            _configuration.GetConnectionString("ReportesDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "No se encontró ConnectionStrings:ReportesDb.");
        }

        return new SqlConnection(connectionString);
    }

    public async Task<PalmReportData> GetReportAsync()
    {
        using var connection = CreateConnection();

        await connection.OpenAsync();

        const string sql = """
SELECT *
FROM dbo.vw_PALM_Dashboard;

SELECT *
FROM dbo.vw_PALM_LatestRun;

SELECT
    Id,
    RunId,
    CustomerName,
    TenantId,
    SubscriptionName,
    SubscriptionId,
    Status,
    Action,
    PartnerIdBefore,
    PartnerIdCurrent,
    PartnerIdTarget,
    PartnerValidationStatus,
    RequiresAction,
    VisibleSubscriptions,
    ClientId,
    ErrorMessage,
    Detail,
    ScanDate,
    UpdatedAt
FROM dbo.vw_PALM_Results
ORDER BY
    RequiresAction DESC,
    CustomerName;

SELECT
    Id,
    RunId,
    CustomerName,
    TenantId,
    SubscriptionName,
    SubscriptionId,
    Status,
    Action,
    PartnerIdBefore,
    PartnerIdCurrent,
    PartnerIdTarget,
    PartnerValidationStatus,
    RequiresAction,
    VisibleSubscriptions,
    ClientId,
    ErrorMessage,
    Detail,
    ScanDate,
    UpdatedAt
FROM dbo.vw_PALM_RequiresAction
ORDER BY CustomerName;

SELECT
    RunId,
    StartedAt,
    FinishedAt,
    DurationSeconds,
    TotalCustomers,
    TotalSubscriptions,
    TotalOK,
    TotalNOK,
    SuccessPercent,
    Status,
    Detail,
    CreatedAt
FROM dbo.vw_PALM_RunHistory
ORDER BY StartedAt DESC;
""";

        using var result = await connection.QueryMultipleAsync(
            sql,
            commandTimeout: 120);

        return new PalmReportData
        {
            Dashboard =
                await result.ReadFirstOrDefaultAsync<PalmDashboard>()
                ?? new PalmDashboard(),

            LatestRun =
                await result.ReadFirstOrDefaultAsync<PalmRun>(),

            Results =
                (await result.ReadAsync<PalmResult>()).ToList(),

            RequiresAction =
                (await result.ReadAsync<PalmResult>()).ToList(),

            RunHistory =
                (await result.ReadAsync<PalmRun>()).ToList()
        };
    }

    public async Task<PalmDashboard> GetDashboardAsync()
    {
        using var connection = CreateConnection();

        return await connection.QueryFirstOrDefaultAsync<PalmDashboard>(
            "SELECT * FROM dbo.vw_PALM_Dashboard;",
            commandTimeout: 120)
            ?? new PalmDashboard();
    }

    public async Task<List<PalmResult>> GetResultsAsync()
    {
        using var connection = CreateConnection();

        var result = await connection.QueryAsync<PalmResult>(
            """
            SELECT *
            FROM dbo.vw_PALM_Results
            ORDER BY RequiresAction DESC, CustomerName;
            """,
            commandTimeout: 120);

        return result.ToList();
    }

    public async Task<List<PalmResult>> GetRequiresActionAsync()
    {
        using var connection = CreateConnection();

        var result = await connection.QueryAsync<PalmResult>(
            """
            SELECT *
            FROM dbo.vw_PALM_RequiresAction
            ORDER BY CustomerName;
            """,
            commandTimeout: 120);

        return result.ToList();
    }

    public async Task<PalmRun?> GetLatestRunAsync()
    {
        using var connection = CreateConnection();

        return await connection.QueryFirstOrDefaultAsync<PalmRun>(
            "SELECT * FROM dbo.vw_PALM_LatestRun;",
            commandTimeout: 120);
    }

    public async Task<List<PalmRun>> GetRunHistoryAsync()
    {
        using var connection = CreateConnection();

        var result = await connection.QueryAsync<PalmRun>(
            """
            SELECT *
            FROM dbo.vw_PALM_RunHistory
            ORDER BY StartedAt DESC;
            """,
            commandTimeout: 120);

        return result.ToList();
    }
}
'@ | Set-Content `
    -Path (Join-Path $servicesPath "PalmReportService.cs") `
    -Encoding UTF8

# ============================================================
# INFORMACIÓN DEL MÓDULO
# ============================================================

@'
<section class="palm-info">

    <div class="palm-info-intro">
        <span>DOCUMENTACIÓN DEL MÓDULO</span>

        <h2>Reporte PALM / MPOR</h2>

        <p>
            Módulo de validación del Partner Admin Link y MPOR utilizado
            para comprobar la asociación del Partner ID de ITQS en los
            tenants y suscripciones administradas.
        </p>
    </div>

    <div class="palm-info-grid">

        <article>
            <h3>🎯 Objetivo</h3>

            <p>
                Centralizar la validación del Partner ID, el estado PAL/MPOR
                y la visibilidad de las suscripciones administradas.
            </p>

            <p>
                El módulo identifica tenants correctamente vinculados,
                asociaciones faltantes, errores de lectura y clientes que
                requieren intervención.
            </p>
        </article>

        <article>
            <h3>🗄️ Base de Datos</h3>

            <strong>Tablas</strong>
            <ul>
                <li>dbo.MPORValidationResults</li>
                <li>dbo.MPORValidationRuns</li>
            </ul>

            <strong>Vistas</strong>
            <ul>
                <li>dbo.vw_PALM_Dashboard</li>
                <li>dbo.vw_PALM_Results</li>
                <li>dbo.vw_PALM_RequiresAction</li>
                <li>dbo.vw_PALM_LatestRun</li>
                <li>dbo.vw_PALM_RunHistory</li>
            </ul>
        </article>

        <article>
            <h3>☁️ Integraciones</h3>

            <ul>
                <li>Microsoft Partner Center</li>
                <li>Microsoft Azure</li>
                <li>Azure Automation</li>
                <li>Azure SQL Database</li>
                <li>Azure Key Vault</li>
                <li>Azure Managed Identity</li>
                <li>ITQS Support Operations Center</li>
            </ul>
        </article>

    </div>

    <article class="palm-info-wide">
        <h3>🔄 Flujo Operativo</h3>

        <div class="palm-flow">
            <span>Partner Center</span>
            <b>→</b>
            <span>Azure Automation</span>
            <b>→</b>
            <span>Validación MPOR/PAL</span>
            <b>→</b>
            <span>Azure SQL</span>
            <b>→</b>
            <span>Reporte PALM</span>
            <b>→</b>
            <span>Auditoría</span>
        </div>
    </article>

    <article class="palm-info-wide">
        <h3>📊 KPIs utilizados</h3>

        <div class="palm-info-kpis">
            <span>Total clientes</span>
            <span>Clientes correctos</span>
            <span>Requieren acción</span>
            <span>Partner ID correcto</span>
            <span>Sin Partner ID</span>
            <span>Suscripciones visibles</span>
            <span>Cumplimiento</span>
            <span>Última ejecución</span>
        </div>
    </article>

</section>
'@ | Set-Content `
    -Path (Join-Path $componentsPath "PalmInformation.razor") `
    -Encoding UTF8

@'
.palm-info {
    display: grid;
    gap: 18px;
}

.palm-info-intro,
.palm-info article {
    padding: 20px;
    border: 1px solid #d6e2f2;
    border-radius: 16px;
    background: #ffffff;
}

.palm-info-intro {
    background: linear-gradient(135deg, #ffffff, #eef4ff);
}

.palm-info-intro span {
    color: #2563eb;
    font-size: .7rem;
    font-weight: 900;
    letter-spacing: .08em;
}

.palm-info-intro h2 {
    margin: 10px 0 8px;
}

.palm-info-intro p,
.palm-info article p,
.palm-info li {
    color: #475569;
    line-height: 1.6;
}

.palm-info-grid {
    display: grid;
    grid-template-columns: repeat(3, minmax(0, 1fr));
    gap: 16px;
}

.palm-info article h3 {
    margin-top: 0;
    color: #0f172a;
}

.palm-info article strong {
    display: block;
    margin-top: 12px;
    color: #1d4ed8;
}

.palm-flow,
.palm-info-kpis {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 10px;
}

.palm-flow span,
.palm-info-kpis span {
    padding: 10px 14px;
    border: 1px solid #bfdbfe;
    border-radius: 999px;
    background: #eff6ff;
    color: #1d4ed8;
    font-size: .78rem;
    font-weight: 800;
}

@media (max-width: 900px) {
    .palm-info-grid {
        grid-template-columns: 1fr;
    }
}
'@ | Set-Content `
    -Path (Join-Path $componentsPath "PalmInformation.razor.css") `
    -Encoding UTF8

# ============================================================
# PÁGINA PRINCIPAL
# ============================================================

@'
@page "/reporting/palm"
@rendermode InteractiveServer

@using System.IO
@using System.Text
@using Microsoft.JSInterop
@using ITQS.SupportOperationsCenter.Components.Reporting.PALM.Models
@using ITQS.SupportOperationsCenter.Components.Reporting.PALM.Services
@using ITQS.SupportOperationsCenter.Components.Reporting.PALM.Components

@inject IPalmReportService PalmReportService
@inject IJSRuntime JS

<PageTitle>Reporte PALM</PageTitle>

<section class="palm-page">

    <section class="palm-hero">
        <div>
            <span class="palm-eyebrow">PARTNER ADMIN LINK / MPOR</span>

            <h1>Reporte PALM</h1>

            <p>
                Validación centralizada del Partner ID, estado MPOR/PAL
                y visibilidad de suscripciones administradas.
            </p>

            @if (IsLoaded)
            {
                <div class="palm-meta">
                    <span>@Report.Dashboard.TotalCustomers clientes</span>
                    <span>@Report.Dashboard.TotalVisibleSubscriptions suscripciones</span>
                    <span>@Report.Dashboard.CompliancePercent.ToString("N2")% cumplimiento</span>
                </div>
            }
        </div>

        <div class="palm-actions">
            <button type="button"
                    class="palm-info-button"
                    @onclick="OpenInformation">
                ⓘ Información
            </button>

            <button type="button"
                    class="palm-refresh-button"
                    disabled="@IsLoading"
                    @onclick="LoadAsync">
                @(IsLoading ? "Cargando..." : "Actualizar")
            </button>
        </div>
    </section>

    <nav class="palm-tabs">
        <button class="@(ActiveTab == "Dashboard" ? "active" : "")"
                @onclick='() => ChangeTab("Dashboard")'>
            Dashboard
        </button>

        <button class="@(ActiveTab == "Results" ? "active" : "")"
                @onclick='() => ChangeTab("Results")'>
            Resultados
        </button>

        <button class="@(ActiveTab == "Action" ? "active" : "")"
                @onclick='() => ChangeTab("Action")'>
            Requiere acción
        </button>

        <button class="@(ActiveTab == "History" ? "active" : "")"
                @onclick='() => ChangeTab("History")'>
            Histórico
        </button>
    </nav>

    @if (IsLoading)
    {
        <section class="palm-card palm-loading">
            <h3>Cargando Reporte PALM...</h3>
            <p>Consultando los resultados de validación MPOR/PAL.</p>
        </section>
    }
    else if (!string.IsNullOrWhiteSpace(ErrorMessage))
    {
        <section class="palm-card palm-error">
            <h3>No fue posible cargar el módulo</h3>
            <p>@ErrorMessage</p>
        </section>
    }
    else if (ActiveTab == "Dashboard")
    {
        <section class="palm-kpis">

            <button type="button"
                    class="palm-kpi primary"
                    @onclick='() => OpenResults("")'>
                <span>Total clientes</span>
                <strong>@Report.Dashboard.TotalCustomers</strong>
                <small>Tenants evaluados</small>
            </button>

            <button type="button"
                    class="palm-kpi success"
                    @onclick='() => OpenResults("OK")'>
                <span>Clientes correctos</span>
                <strong>@Report.Dashboard.TotalOK</strong>
                <small>Partner ID validado</small>
            </button>

            <button type="button"
                    class="palm-kpi danger"
                    @onclick="OpenAction">
                <span>Requieren acción</span>
                <strong>@Report.Dashboard.RequiresAction</strong>
                <small>Intervención requerida</small>
            </button>

            <button type="button"
                    class="palm-kpi info"
                    @onclick='() => OpenResults("")'>
                <span>Suscripciones visibles</span>
                <strong>@Report.Dashboard.TotalVisibleSubscriptions</strong>
                <small>Detectadas en Azure</small>
            </button>

            <button type="button"
                    class="palm-kpi warning"
                    @onclick="OpenAction">
                <span>Sin Partner ID</span>
                <strong>@Report.Dashboard.WithoutPartnerId</strong>
                <small>Asociación no encontrada</small>
            </button>

            <button type="button"
                    class="palm-kpi compliance"
                    @onclick='() => OpenResults("")'>
                <span>Cumplimiento</span>
                <strong>@Report.Dashboard.CompliancePercent.ToString("N2")%</strong>
                <small>Estado global MPOR/PAL</small>
            </button>

        </section>

        <section class="palm-dashboard-grid">

            <article class="palm-card">
                <div class="palm-card-header">
                    <div>
                        <span class="palm-card-eyebrow">ESTADO OPERATIVO</span>
                        <h2>Resumen de validación</h2>
                    </div>
                </div>

                <div class="palm-summary-list">
                    <div>
                        <span>Partner ID correcto</span>
                        <strong>@Report.Dashboard.PartnerIdCorrect</strong>
                    </div>

                    <div>
                        <span>Partner ID diferente</span>
                        <strong>@Report.Dashboard.DifferentPartnerId</strong>
                    </div>

                    <div>
                        <span>Sin Partner ID</span>
                        <strong>@Report.Dashboard.WithoutPartnerId</strong>
                    </div>

                    <div>
                        <span>Refrescados</span>
                        <strong>@Report.Dashboard.TotalRefreshed</strong>
                    </div>
                </div>
            </article>

            <article class="palm-card">
                <div class="palm-card-header">
                    <div>
                        <span class="palm-card-eyebrow">ÚLTIMA EJECUCIÓN</span>
                        <h2>Azure Automation</h2>
                    </div>
                </div>

                @if (Report.LatestRun is null)
                {
                    <p>No existe una ejecución registrada.</p>
                }
                else
                {
                    <div class="palm-run-detail">
                        <div>
                            <span>Estado</span>
                            <strong>@Report.LatestRun.Status</strong>
                        </div>

                        <div>
                            <span>Inicio</span>
                            <strong>@FormatDate(Report.LatestRun.StartedAt)</strong>
                        </div>

                        <div>
                            <span>Duración</span>
                            <strong>@FormatDuration(Report.LatestRun.DurationSeconds)</strong>
                        </div>

                        <div>
                            <span>Clientes</span>
                            <strong>@Report.LatestRun.TotalCustomers</strong>
                        </div>

                        <div>
                            <span>Suscripciones</span>
                            <strong>@Report.LatestRun.TotalSubscriptions</strong>
                        </div>
                    </div>
                }
            </article>

        </section>

        @if (Report.RequiresAction.Count > 0)
        {
            <section class="palm-card">
                <div class="palm-card-header">
                    <div>
                        <span class="palm-card-eyebrow danger-text">ATENCIÓN</span>
                        <h2>Clientes que requieren intervención</h2>
                    </div>

                    <button type="button"
                            class="palm-secondary-button"
                            @onclick="OpenAction">
                        Ver casos
                    </button>
                </div>

                <div class="palm-alert-list">
                    @foreach (var item in Report.RequiresAction.Take(5))
                    {
                        <div>
                            <strong>@item.CustomerName</strong>
                            <span>@Normalize(item.ErrorMessage)</span>
                        </div>
                    }
                </div>
            </section>
        }
    }
    else if (ActiveTab == "Results")
    {
        <section class="palm-card">

            <div class="palm-table-heading">
                <div>
                    <span class="palm-card-eyebrow">INVENTARIO PALM</span>
                    <h2>Resultados de validación</h2>
                    <p>@FilteredResults.Count registros encontrados.</p>
                </div>

                <button type="button"
                        class="palm-export-button"
                        @onclick="ExportResultsCsvAsync">
                    ↓ Exportar CSV
                </button>
            </div>

            <div class="palm-filters">
                <input value="@SearchText"
                       @oninput="OnSearchChanged"
                       placeholder="Buscar cliente, tenant o Partner ID..." />

                <select value="@StatusFilter"
                        @onchange="OnStatusChanged">
                    <option value="">Todos los estados</option>
                    <option value="OK">OK</option>
                    <option value="NOK">NOK</option>
                </select>

                <select value="@PartnerFilter"
                        @onchange="OnPartnerChanged">
                    <option value="">Todas las validaciones</option>
                    <option value="PARTNER_ID_CORRECTO">Partner ID correcto</option>
                    <option value="SIN_PARTNER_ID">Sin Partner ID</option>
                    <option value="PARTNER_ID_DIFERENTE">Partner ID diferente</option>
                </select>
            </div>

            <div class="palm-table-wrapper">
                <table class="palm-table">
                    <thead>
                        <tr>
                            <th>Cliente</th>
                            <th>Estado</th>
                            <th>Acción</th>
                            <th>Partner anterior</th>
                            <th>Partner actual</th>
                            <th>Partner objetivo</th>
                            <th>Suscripciones</th>
                            <th>Validación</th>
                            <th>Fecha</th>
                            <th>Detalle</th>
                        </tr>
                    </thead>

                    <tbody>
                        @foreach (var item in PagedResults)
                        {
                            <tr>
                                <td>
                                    <strong>@item.CustomerName</strong>
                                    <small>@item.TenantId</small>
                                </td>

                                <td>
                                    <span class="palm-status @GetStatusClass(item.Status)">
                                        @item.Status
                                    </span>
                                </td>

                                <td>@item.Action</td>
                                <td>@Normalize(item.PartnerIdBefore)</td>
                                <td>@Normalize(item.PartnerIdCurrent)</td>
                                <td>@Normalize(item.PartnerIdTarget)</td>
                                <td>@(item.VisibleSubscriptions ?? 0)</td>

                                <td>
                                    <span class="palm-validation @GetValidationClass(item.PartnerValidationStatus)">
                                        @GetValidationLabel(item.PartnerValidationStatus)
                                    </span>
                                </td>

                                <td>@FormatDate(item.ScanDate)</td>

                                <td>
                                    <button type="button"
                                            class="palm-view-button"
                                            @onclick="() => OpenDetail(item)">
                                        Ver
                                    </button>
                                </td>
                            </tr>
                        }
                    </tbody>
                </table>
            </div>

            @Pagination()
        </section>
    }
    else if (ActiveTab == "Action")
    {
        <section class="palm-card">

            <div class="palm-table-heading">
                <div>
                    <span class="palm-card-eyebrow danger-text">CENTRO OPERATIVO</span>
                    <h2>Clientes que requieren acción</h2>
                    <p>@Report.RequiresAction.Count casos requieren intervención.</p>
                </div>

                <button type="button"
                        class="palm-export-button"
                        @onclick="ExportActionCsvAsync">
                    ↓ Exportar CSV
                </button>
            </div>

            <div class="palm-action-grid">
                @foreach (var item in Report.RequiresAction)
                {
                    <article class="palm-action-card">
                        <div class="palm-action-title">
                            <div>
                                <strong>@item.CustomerName</strong>
                                <small>@item.TenantId</small>
                            </div>

                            <span>@item.Status</span>
                        </div>

                        <dl>
                            <div>
                                <dt>Acción</dt>
                                <dd>@item.Action</dd>
                            </div>

                            <div>
                                <dt>Partner actual</dt>
                                <dd>@Normalize(item.PartnerIdCurrent)</dd>
                            </div>

                            <div>
                                <dt>Partner objetivo</dt>
                                <dd>@Normalize(item.PartnerIdTarget)</dd>
                            </div>

                            <div>
                                <dt>Suscripciones visibles</dt>
                                <dd>@(item.VisibleSubscriptions ?? 0)</dd>
                            </div>
                        </dl>

                        <div class="palm-error-message">
                            @Normalize(item.ErrorMessage)
                        </div>

                        <div class="palm-recommendation">
                            <strong>Recomendación</strong>

                            <p>
                                Validar que el usuario o Service Principal utilizado
                                por el runbook esté correctamente vinculado con el
                                Partner ID @item.PartnerIdTarget.
                            </p>
                        </div>

                        <button type="button"
                                class="palm-view-button"
                                @onclick="() => OpenDetail(item)">
                            Ver detalle
                        </button>
                    </article>
                }
            </div>
        </section>
    }
    else if (ActiveTab == "History")
    {
        <section class="palm-card">

            <div class="palm-table-heading">
                <div>
                    <span class="palm-card-eyebrow">AUDITORÍA</span>
                    <h2>Histórico de ejecuciones</h2>
                    <p>@Report.RunHistory.Count ejecuciones registradas.</p>
                </div>

                <button type="button"
                        class="palm-export-button"
                        @onclick="ExportHistoryCsvAsync">
                    ↓ Exportar CSV
                </button>
            </div>

            <div class="palm-table-wrapper">
                <table class="palm-table">
                    <thead>
                        <tr>
                            <th>Run ID</th>
                            <th>Inicio</th>
                            <th>Finalización</th>
                            <th>Duración</th>
                            <th>Clientes</th>
                            <th>Suscripciones</th>
                            <th>OK</th>
                            <th>NOK</th>
                            <th>Éxito</th>
                            <th>Estado</th>
                        </tr>
                    </thead>

                    <tbody>
                        @foreach (var run in Report.RunHistory)
                        {
                            <tr>
                                <td><small>@run.RunId</small></td>
                                <td>@FormatDate(run.StartedAt)</td>
                                <td>@FormatDate(run.FinishedAt)</td>
                                <td>@FormatDuration(run.DurationSeconds)</td>
                                <td>@run.TotalCustomers</td>
                                <td>@run.TotalSubscriptions</td>
                                <td>@run.TotalOK</td>
                                <td>@run.TotalNOK</td>
                                <td>@run.SuccessPercent.ToString("N2")%</td>

                                <td>
                                    <span class="palm-status @GetRunStatusClass(run.Status)">
                                        @run.Status
                                    </span>
                                </td>
                            </tr>
                        }
                    </tbody>
                </table>
            </div>
        </section>
    }

</section>

@if (ShowInformation)
{
    <div class="palm-modal-backdrop"
         @onclick="CloseInformation">
    </div>

    <section class="palm-modal"
             role="dialog"
             aria-modal="true">

        <header>
            <div>
                <span>DOCUMENTACIÓN TÉCNICA</span>
                <h2>Reporte PALM / MPOR</h2>
            </div>

            <button type="button"
                    @onclick="CloseInformation">
                ×
            </button>
        </header>

        <div class="palm-modal-content">
            <PalmInformation />
        </div>

    </section>
}

@if (SelectedResult is not null)
{
    <div class="palm-modal-backdrop"
         @onclick="CloseDetail">
    </div>

    <section class="palm-detail-modal"
             role="dialog"
             aria-modal="true">

        <header>
            <div>
                <span>DETALLE PALM</span>
                <h2>@SelectedResult.CustomerName</h2>
            </div>

            <button type="button"
                    @onclick="CloseDetail">
                ×
            </button>
        </header>

        <div class="palm-detail-content">

            <dl>
                <div>
                    <dt>Tenant ID</dt>
                    <dd>@SelectedResult.TenantId</dd>
                </div>

                <div>
                    <dt>Client ID</dt>
                    <dd>@Normalize(SelectedResult.ClientId)</dd>
                </div>

                <div>
                    <dt>Estado</dt>
                    <dd>@SelectedResult.Status</dd>
                </div>

                <div>
                    <dt>Acción</dt>
                    <dd>@SelectedResult.Action</dd>
                </div>

                <div>
                    <dt>Partner ID anterior</dt>
                    <dd>@Normalize(SelectedResult.PartnerIdBefore)</dd>
                </div>

                <div>
                    <dt>Partner ID actual</dt>
                    <dd>@Normalize(SelectedResult.PartnerIdCurrent)</dd>
                </div>

                <div>
                    <dt>Partner ID objetivo</dt>
                    <dd>@Normalize(SelectedResult.PartnerIdTarget)</dd>
                </div>

                <div>
                    <dt>Suscripciones visibles</dt>
                    <dd>@(SelectedResult.VisibleSubscriptions ?? 0)</dd>
                </div>

                <div>
                    <dt>Fecha de validación</dt>
                    <dd>@FormatDate(SelectedResult.ScanDate)</dd>
                </div>
            </dl>

            @if (!string.IsNullOrWhiteSpace(SelectedResult.ErrorMessage))
            {
                <section class="palm-detail-error">
                    <strong>Error detectado</strong>
                    <p>@SelectedResult.ErrorMessage</p>
                </section>
            }

            <section class="palm-detail-text">
                <strong>Detalle de ejecución</strong>
                <p>@Normalize(SelectedResult.Detail)</p>
            </section>

        </div>
    </section>
}

@code {
    private PalmReportData Report { get; set; } = new();

    private bool IsLoading { get; set; } = true;
    private bool IsLoaded { get; set; }
    private bool ShowInformation { get; set; }

    private string ActiveTab { get; set; } = "Dashboard";
    private string SearchText { get; set; } = string.Empty;
    private string StatusFilter { get; set; } = string.Empty;
    private string PartnerFilter { get; set; } = string.Empty;
    private string? ErrorMessage { get; set; }

    private int CurrentPage { get; set; } = 1;
    private int PageSize { get; set; } = 20;

    private PalmResult? SelectedResult { get; set; }

    private List<PalmResult> FilteredResults =>
        Report.Results
            .Where(item =>
                string.IsNullOrWhiteSpace(SearchText) ||
                Contains(item.CustomerName, SearchText) ||
                Contains(item.TenantId.ToString(), SearchText) ||
                Contains(item.PartnerIdCurrent, SearchText) ||
                Contains(item.PartnerIdTarget, SearchText))
            .Where(item =>
                string.IsNullOrWhiteSpace(StatusFilter) ||
                string.Equals(
                    item.Status,
                    StatusFilter,
                    StringComparison.OrdinalIgnoreCase))
            .Where(item =>
                string.IsNullOrWhiteSpace(PartnerFilter) ||
                string.Equals(
                    item.PartnerValidationStatus,
                    PartnerFilter,
                    StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.RequiresAction)
            .ThenBy(item => item.CustomerName)
            .ToList();

    private List<PalmResult> PagedResults =>
        FilteredResults
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

    private int TotalPages =>
        Math.Max(
            1,
            (int)Math.Ceiling(
                FilteredResults.Count / (double)PageSize));

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            Report = await PalmReportService.GetReportAsync();
            IsLoaded = true;
            CurrentPage = 1;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ChangeTab(string tab)
    {
        ActiveTab = tab;
        CurrentPage = 1;
    }

    private void OpenResults(string status)
    {
        StatusFilter = status;
        PartnerFilter = string.Empty;
        SearchText = string.Empty;
        ActiveTab = "Results";
        CurrentPage = 1;
    }

    private void OpenAction()
    {
        ActiveTab = "Action";
        CurrentPage = 1;
    }

    private void OpenInformation()
    {
        ShowInformation = true;
    }

    private void CloseInformation()
    {
        ShowInformation = false;
    }

    private void OpenDetail(PalmResult item)
    {
        SelectedResult = item;
    }

    private void CloseDetail()
    {
        SelectedResult = null;
    }

    private void OnSearchChanged(ChangeEventArgs args)
    {
        SearchText = args.Value?.ToString() ?? string.Empty;
        CurrentPage = 1;
    }

    private void OnStatusChanged(ChangeEventArgs args)
    {
        StatusFilter = args.Value?.ToString() ?? string.Empty;
        CurrentPage = 1;
    }

    private void OnPartnerChanged(ChangeEventArgs args)
    {
        PartnerFilter = args.Value?.ToString() ?? string.Empty;
        CurrentPage = 1;
    }

    private void GoToPage(int page)
    {
        CurrentPage = Math.Clamp(page, 1, TotalPages);
    }

    private RenderFragment Pagination() => builder =>
    {
        var sequence = 0;

        builder.OpenElement(sequence++, "div");
        builder.AddAttribute(sequence++, "class", "palm-pagination");

        builder.OpenElement(sequence++, "button");
        builder.AddAttribute(sequence++, "type", "button");
        builder.AddAttribute(sequence++, "disabled", CurrentPage <= 1);
        builder.AddAttribute(
            sequence++,
            "onclick",
            EventCallback.Factory.Create(
                this,
                () => GoToPage(CurrentPage - 1)));
        builder.AddContent(sequence++, "‹");
        builder.CloseElement();

        builder.OpenElement(sequence++, "span");
        builder.AddContent(
            sequence++,
            $"Página {CurrentPage} de {TotalPages}");
        builder.CloseElement();

        builder.OpenElement(sequence++, "button");
        builder.AddAttribute(sequence++, "type", "button");
        builder.AddAttribute(sequence++, "disabled", CurrentPage >= TotalPages);
        builder.AddAttribute(
            sequence++,
            "onclick",
            EventCallback.Factory.Create(
                this,
                () => GoToPage(CurrentPage + 1)));
        builder.AddContent(sequence++, "›");
        builder.CloseElement();

        builder.CloseElement();
    };

    private async Task ExportResultsCsvAsync()
    {
        await ExportResultsAsync(
            FilteredResults,
            $"PALM_Resultados_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
    }

    private async Task ExportActionCsvAsync()
    {
        await ExportResultsAsync(
            Report.RequiresAction,
            $"PALM_RequiereAccion_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
    }

    private async Task ExportResultsAsync(
        IEnumerable<PalmResult> rows,
        string fileName)
    {
        var csv = new StringBuilder();

        csv.AppendLine(
            "Cliente,TenantId,Estado,Accion,PartnerIdAnterior," +
            "PartnerIdActual,PartnerIdObjetivo,Validacion," +
            "SuscripcionesVisibles,ClientId,Error,Detalle,Fecha");

        foreach (var item in rows)
        {
            csv.AppendLine(string.Join(",",
                Csv(item.CustomerName),
                Csv(item.TenantId.ToString()),
                Csv(item.Status),
                Csv(item.Action),
                Csv(item.PartnerIdBefore),
                Csv(item.PartnerIdCurrent),
                Csv(item.PartnerIdTarget),
                Csv(GetValidationLabel(item.PartnerValidationStatus)),
                Csv(item.VisibleSubscriptions?.ToString()),
                Csv(item.ClientId),
                Csv(item.ErrorMessage),
                Csv(item.Detail),
                Csv(item.ScanDate.ToString("yyyy-MM-dd HH:mm:ss"))
            ));
        }

        await DownloadCsvAsync(fileName, csv.ToString());
    }

    private async Task ExportHistoryCsvAsync()
    {
        var csv = new StringBuilder();

        csv.AppendLine(
            "RunId,Inicio,Finalizacion,DuracionSegundos,Clientes," +
            "Suscripciones,OK,NOK,PorcentajeExito,Estado,Detalle");

        foreach (var run in Report.RunHistory)
        {
            csv.AppendLine(string.Join(",",
                Csv(run.RunId.ToString()),
                Csv(run.StartedAt.ToString("yyyy-MM-dd HH:mm:ss")),
                Csv(run.FinishedAt?.ToString("yyyy-MM-dd HH:mm:ss")),
                Csv(run.DurationSeconds?.ToString()),
                Csv(run.TotalCustomers.ToString()),
                Csv(run.TotalSubscriptions.ToString()),
                Csv(run.TotalOK.ToString()),
                Csv(run.TotalNOK.ToString()),
                Csv(run.SuccessPercent.ToString("N2")),
                Csv(run.Status),
                Csv(run.Detail)
            ));
        }

        await DownloadCsvAsync(
            $"PALM_Historico_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
            csv.ToString());
    }

    private async Task DownloadCsvAsync(
        string fileName,
        string content)
    {
        var bytes = Encoding.UTF8.GetBytes(
            "\uFEFF" + content);

        using var stream = new MemoryStream(bytes);
        using var streamReference =
            new DotNetStreamReference(stream);

        await JS.InvokeVoidAsync(
            "downloadFileFromStream",
            fileName,
            streamReference);
    }

    private static string Csv(string? value)
    {
        value ??= string.Empty;
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static bool Contains(
        string? value,
        string search)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.Contains(
                   search,
                   StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "-"
            : value;
    }

    private static string FormatDate(DateTime? value)
    {
        return value?.ToLocalTime()
            .ToString("dd/MM/yyyy HH:mm")
            ?? "-";
    }

    private static string FormatDuration(int? seconds)
    {
        if (!seconds.HasValue)
        {
            return "-";
        }

        var duration =
            TimeSpan.FromSeconds(seconds.Value);

        return duration.TotalMinutes >= 1
            ? $"{(int)duration.TotalMinutes}m {duration.Seconds}s"
            : $"{duration.Seconds}s";
    }

    private static string GetStatusClass(string? status) =>
        status?.ToUpperInvariant() switch
        {
            "OK" => "success",
            "NOK" => "danger",
            _ => "neutral"
        };

    private static string GetRunStatusClass(string? status) =>
        status?.ToUpperInvariant() switch
        {
            "COMPLETED" => "success",
            "COMPLETEDWITHERRORS" => "warning",
            "RUNNING" => "info",
            "FAILED" => "danger",
            _ => "neutral"
        };

    private static string GetValidationClass(string? status) =>
        status?.ToUpperInvariant() switch
        {
            "PARTNER_ID_CORRECTO" => "success",
            "SIN_PARTNER_ID" => "danger",
            "PARTNER_ID_DIFERENTE" => "warning",
            _ => "neutral"
        };

    private static string GetValidationLabel(string? status) =>
        status?.ToUpperInvariant() switch
        {
            "PARTNER_ID_CORRECTO" => "Partner ID correcto",
            "SIN_PARTNER_ID" => "Sin Partner ID",
            "PARTNER_ID_DIFERENTE" => "Partner ID diferente",
            _ => "Sin clasificar"
        };
}
'@ | Set-Content `
    -Path (Join-Path $moduleRoot "PalmReport.razor") `
    -Encoding UTF8

# ============================================================
# CSS PRINCIPAL
# ============================================================

@'
.palm-page {
    display: grid;
    gap: 16px;
    padding: 20px;
    background: #f4f7fb;
    min-height: 100vh;
}

.palm-hero {
    display: flex;
    justify-content: space-between;
    gap: 24px;
    padding: 26px;
    border-radius: 22px;
    background: linear-gradient(120deg, #172554, #1d4ed8 58%, #06b6d4);
    color: #ffffff;
    box-shadow: 0 18px 45px rgba(15, 23, 42, .16);
}

.palm-hero h1 {
    margin: 10px 0 8px;
    font-size: 2rem;
}

.palm-hero p {
    max-width: 720px;
    margin: 0;
    line-height: 1.5;
}

.palm-eyebrow,
.palm-card-eyebrow {
    font-size: .7rem;
    font-weight: 900;
    letter-spacing: .08em;
}

.palm-eyebrow {
    display: inline-flex;
    padding: 6px 12px;
    border-radius: 999px;
    background: rgba(255, 255, 255, .15);
}

.palm-meta {
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
    margin-top: 18px;
}

.palm-meta span {
    padding: 8px 12px;
    border-radius: 10px;
    background: rgba(255, 255, 255, .16);
    font-size: .78rem;
    font-weight: 800;
}

.palm-actions {
    display: flex;
    align-items: flex-start;
    gap: 10px;
}

.palm-actions button,
.palm-export-button,
.palm-secondary-button {
    min-height: 38px;
    padding: 9px 16px;
    border-radius: 11px;
    font-weight: 850;
    cursor: pointer;
}

.palm-info-button {
    border: 1px solid rgba(255, 255, 255, .35);
    background: rgba(255, 255, 255, .14);
    color: #ffffff;
}

.palm-refresh-button {
    border: 0;
    background: #ffffff;
    color: #1d4ed8;
}

.palm-tabs {
    display: inline-flex;
    width: fit-content;
    max-width: 100%;
    gap: 4px;
    padding: 6px;
    overflow-x: auto;
    border-radius: 16px;
    background: #edf2f8;
}

.palm-tabs button {
    padding: 10px 17px;
    border: 0;
    border-radius: 11px;
    background: transparent;
    color: #334155;
    font-weight: 850;
    cursor: pointer;
    white-space: nowrap;
}

.palm-tabs button.active {
    background: #ffffff;
    color: #0f172a;
    box-shadow: 0 5px 14px rgba(15, 23, 42, .08);
}

.palm-card {
    padding: 20px;
    border: 1px solid #d8e2ef;
    border-radius: 18px;
    background: #ffffff;
    box-shadow: 0 8px 22px rgba(15, 23, 42, .04);
}

.palm-loading,
.palm-error {
    min-height: 160px;
}

.palm-error {
    border-color: #fecaca;
    background: #fff7f7;
}

.palm-kpis {
    display: grid;
    grid-template-columns: repeat(6, minmax(0, 1fr));
    gap: 14px;
}

.palm-kpi {
    display: grid;
    gap: 7px;
    min-height: 132px;
    padding: 17px;
    text-align: left;
    border: 1px solid #dbe5f1;
    border-left-width: 5px;
    border-radius: 16px;
    background: #ffffff;
    cursor: pointer;
}

.palm-kpi span,
.palm-kpi small {
    color: #64748b;
    font-weight: 750;
}

.palm-kpi strong {
    color: #0f172a;
    font-size: 1.8rem;
}

.palm-kpi.primary {
    border-left-color: #2563eb;
}

.palm-kpi.success {
    border-left-color: #16a34a;
}

.palm-kpi.danger {
    border-left-color: #dc2626;
}

.palm-kpi.info {
    border-left-color: #0891b2;
}

.palm-kpi.warning {
    border-left-color: #f59e0b;
}

.palm-kpi.compliance {
    border-left-color: #7c3aed;
}

.palm-dashboard-grid {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 16px;
}

.palm-card-header,
.palm-table-heading {
    display: flex;
    justify-content: space-between;
    gap: 16px;
    margin-bottom: 17px;
}

.palm-card-header h2,
.palm-table-heading h2 {
    margin: 5px 0;
}

.palm-card-eyebrow {
    color: #2563eb;
}

.danger-text {
    color: #dc2626;
}

.palm-summary-list,
.palm-run-detail {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 12px;
}

.palm-summary-list div,
.palm-run-detail div {
    display: grid;
    gap: 5px;
    padding: 13px;
    border: 1px solid #e2e8f0;
    border-radius: 12px;
    background: #f8fafc;
}

.palm-summary-list span,
.palm-run-detail span {
    color: #64748b;
    font-size: .78rem;
    font-weight: 750;
}

.palm-alert-list {
    display: grid;
    gap: 10px;
}

.palm-alert-list div {
    display: grid;
    gap: 5px;
    padding: 13px;
    border-left: 4px solid #dc2626;
    border-radius: 10px;
    background: #fff7f7;
}

.palm-alert-list span {
    color: #7f1d1d;
    font-size: .82rem;
}

.palm-secondary-button,
.palm-export-button {
    border: 0;
    background: #2563eb;
    color: #ffffff;
}

.palm-filters {
    display: grid;
    grid-template-columns: 2fr 1fr 1fr;
    gap: 12px;
    margin-bottom: 16px;
}

.palm-filters input,
.palm-filters select {
    min-height: 42px;
    padding: 9px 12px;
    border: 1px solid #cfdbea;
    border-radius: 11px;
    background: #ffffff;
}

.palm-table-wrapper {
    overflow-x: auto;
}

.palm-table {
    width: 100%;
    border-collapse: collapse;
    min-width: 1100px;
}

.palm-table th {
    padding: 12px;
    text-align: left;
    border-bottom: 1px solid #cfdbea;
    background: #f8fafc;
    color: #334155;
    font-size: .72rem;
    text-transform: uppercase;
}

.palm-table td {
    padding: 12px;
    border-bottom: 1px solid #edf2f7;
    vertical-align: top;
    color: #334155;
    font-size: .8rem;
}

.palm-table td:first-child {
    display: grid;
    gap: 4px;
    min-width: 190px;
}

.palm-table small {
    color: #64748b;
}

.palm-status,
.palm-validation {
    display: inline-flex;
    padding: 6px 9px;
    border-radius: 999px;
    font-size: .68rem;
    font-weight: 900;
}

.palm-status.success,
.palm-validation.success {
    background: #dcfce7;
    color: #166534;
}

.palm-status.danger,
.palm-validation.danger {
    background: #fee2e2;
    color: #991b1b;
}

.palm-status.warning,
.palm-validation.warning {
    background: #fef3c7;
    color: #92400e;
}

.palm-status.info {
    background: #dbeafe;
    color: #1d4ed8;
}

.palm-status.neutral,
.palm-validation.neutral {
    background: #e2e8f0;
    color: #475569;
}

.palm-view-button {
    padding: 7px 12px;
    border: 1px solid #bfdbfe;
    border-radius: 9px;
    background: #eff6ff;
    color: #1d4ed8;
    font-weight: 850;
    cursor: pointer;
}

.palm-pagination {
    display: flex;
    justify-content: center;
    align-items: center;
    gap: 12px;
    margin-top: 17px;
}

.palm-pagination button {
    width: 36px;
    height: 36px;
    border: 1px solid #d5dfec;
    border-radius: 9px;
    background: #ffffff;
    cursor: pointer;
}

.palm-action-grid {
    display: grid;
    grid-template-columns: repeat(3, minmax(0, 1fr));
    gap: 15px;
}

.palm-action-card {
    display: grid;
    gap: 14px;
    padding: 17px;
    border: 1px solid #fecaca;
    border-top: 5px solid #dc2626;
    border-radius: 15px;
    background: #ffffff;
}

.palm-action-title {
    display: flex;
    justify-content: space-between;
    gap: 12px;
}

.palm-action-title div {
    display: grid;
    gap: 4px;
}

.palm-action-title small {
    color: #64748b;
}

.palm-action-title > span {
    height: fit-content;
    padding: 5px 8px;
    border-radius: 999px;
    background: #fee2e2;
    color: #991b1b;
    font-size: .68rem;
    font-weight: 900;
}

.palm-action-card dl,
.palm-detail-content dl {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 9px;
    margin: 0;
}

.palm-action-card dl div,
.palm-detail-content dl div {
    display: grid;
    gap: 4px;
    padding: 10px;
    border-radius: 10px;
    background: #f8fafc;
}

.palm-action-card dt,
.palm-detail-content dt {
    color: #64748b;
    font-size: .7rem;
    font-weight: 800;
}

.palm-action-card dd,
.palm-detail-content dd {
    margin: 0;
    overflow-wrap: anywhere;
}

.palm-error-message {
    padding: 11px;
    border-radius: 10px;
    background: #fff1f2;
    color: #9f1239;
    font-size: .78rem;
    line-height: 1.5;
}

.palm-recommendation {
    padding: 12px;
    border-radius: 11px;
    background: #eff6ff;
    color: #1e3a8a;
}

.palm-recommendation p {
    margin-bottom: 0;
    font-size: .8rem;
    line-height: 1.5;
}

.palm-modal-backdrop {
    position: fixed;
    inset: 0;
    z-index: 9998;
    background: rgba(15, 23, 42, .65);
    backdrop-filter: blur(4px);
}

.palm-modal,
.palm-detail-modal {
    position: fixed;
    top: 50%;
    left: 50%;
    z-index: 9999;
    display: flex;
    flex-direction: column;
    width: min(1450px, calc(100vw - 42px));
    height: min(900px, calc(100vh - 42px));
    transform: translate(-50%, -50%);
    overflow: hidden;
    border-radius: 20px;
    background: #f7faff;
    box-shadow: 0 30px 90px rgba(15, 23, 42, .42);
}

.palm-detail-modal {
    width: min(850px, calc(100vw - 42px));
    height: auto;
    max-height: calc(100vh - 42px);
}

.palm-modal > header,
.palm-detail-modal > header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 17px 22px;
    border-bottom: 1px solid #d8e2ef;
    background: #ffffff;
}

.palm-modal header span,
.palm-detail-modal header span {
    color: #2563eb;
    font-size: .68rem;
    font-weight: 900;
    letter-spacing: .08em;
}

.palm-modal header h2,
.palm-detail-modal header h2 {
    margin: 4px 0 0;
}

.palm-modal header button,
.palm-detail-modal header button {
    width: 38px;
    height: 38px;
    border: 1px solid #d5dfec;
    border-radius: 10px;
    background: #ffffff;
    font-size: 1.5rem;
    cursor: pointer;
}

.palm-modal-content,
.palm-detail-content {
    overflow-y: auto;
    padding: 20px;
}

.palm-detail-content {
    display: grid;
    gap: 15px;
}

.palm-detail-error,
.palm-detail-text {
    padding: 14px;
    border-radius: 12px;
}

.palm-detail-error {
    background: #fff1f2;
    color: #9f1239;
}

.palm-detail-text {
    background: #f1f5f9;
    color: #334155;
}

@media (max-width: 1250px) {
    .palm-kpis {
        grid-template-columns: repeat(3, minmax(0, 1fr));
    }

    .palm-action-grid {
        grid-template-columns: repeat(2, minmax(0, 1fr));
    }
}

@media (max-width: 800px) {
    .palm-page {
        padding: 12px;
    }

    .palm-hero,
    .palm-card-header,
    .palm-table-heading {
        flex-direction: column;
    }

    .palm-actions {
        align-items: center;
    }

    .palm-kpis,
    .palm-dashboard-grid,
    .palm-action-grid,
    .palm-filters {
        grid-template-columns: 1fr;
    }
}
'@ | Set-Content `
    -Path (Join-Path $moduleRoot "PalmReport.razor.css") `
    -Encoding UTF8

# ============================================================
# REGISTRAR SERVICIO EN PROGRAM.CS
# ============================================================

$programFile = Join-Path $root "Program.cs"
$programContent = Get-Content $programFile -Raw

$serviceUsing =
    "using ITQS.SupportOperationsCenter.Components.Reporting.PALM.Services;"

if ($programContent -notmatch [regex]::Escape($serviceUsing))
{
    $programContent =
        $serviceUsing + "`r`n" + $programContent
}

$registration =
    "builder.Services.AddScoped<IPalmReportService, PalmReportService>();"

if ($programContent -notmatch [regex]::Escape($registration))
{
    $marker = "var app = builder.Build();"

    if (-not $programContent.Contains($marker))
    {
        throw "No se encontró 'var app = builder.Build();' en Program.cs."
    }

    $programContent = $programContent.Replace(
        $marker,
        $registration + "`r`n`r`n" + $marker
    )
}

Set-Content `
    -Path $programFile `
    -Value $programContent `
    -Encoding UTF8

Write-Host ""
Write-Host "============================================================"
Write-Host "MÓDULO PALM GENERADO CORRECTAMENTE"
Write-Host "============================================================"
Write-Host "Ruta: /reporting/palm"
Write-Host ""
Write-Host "Archivos creados:"
Write-Host " - Models\PalmModels.cs"
Write-Host " - Services\IPalmReportService.cs"
Write-Host " - Services\PalmReportService.cs"
Write-Host " - Components\PalmInformation.razor"
Write-Host " - Components\PalmInformation.razor.css"
Write-Host " - PalmReport.razor"
Write-Host " - PalmReport.razor.css"
Write-Host ""
'@ | Set-Content $scriptFile -Encoding UTF8