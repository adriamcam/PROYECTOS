using Dapper;
using ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Models.Metrics;
using ITQS.SupportOperationsCenter.Data;

namespace ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Services.Metrics;

public sealed class ClientVmMetricsService
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public ClientVmMetricsService(
        ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<ClientVmMetricModel>>
        GetMetricsAsync(
            string subscriptionId,
            int days)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            return [];
        }

        days = NormalizeDays(days);

        const string sql = """
            WITH PeriodMetrics AS
            (
                SELECT
                    CustomerName,
                    TenantId,
                    SubscriptionId,
                    SubscriptionName,
                    ResourceGroup,
                    Computer,
                    FechaMetrica,
                    CpuAvgPercent,
                    CpuMaxPercent,
                    CpuP95Percent,
                    MemoryUsedAvgPercent,
                    MemoryUsedMaxPercent,
                    MemoryUsedP95Percent,
                    AvailabilityRate,
                    VMStatus
                FROM dbo.VmPerformanceDaily
                WHERE TRY_CONVERT(
                        uniqueidentifier,
                        SubscriptionId
                      ) = TRY_CONVERT(
                        uniqueidentifier,
                        @SubscriptionId
                      )
                  AND FechaMetrica >=
                      DATEADD(
                          DAY,
                          -(@Days - 1),
                          CONVERT(date, SYSUTCDATETIME())
                      )
            ),
            Aggregated AS
            (
                SELECT
                    MAX(CustomerName) AS CustomerName,
                    TenantId,
                    SubscriptionId,
                    MAX(SubscriptionName) AS SubscriptionName,
                    MAX(ResourceGroup) AS ResourceGroup,
                    Computer,
                    MIN(FechaMetrica) AS Desde,
                    MAX(FechaMetrica) AS Hasta,
                    COUNT(DISTINCT FechaMetrica) AS DiasConDatos,

                    AVG(CpuAvgPercent) AS CpuPromedio,
                    MAX(CpuMaxPercent) AS CpuMaximo,
                    AVG(CpuP95Percent) AS CpuP95,

                    AVG(MemoryUsedAvgPercent) AS MemoriaPromedio,
                    MAX(MemoryUsedMaxPercent) AS MemoriaMaxima,
                    AVG(MemoryUsedP95Percent) AS MemoriaP95,

                    AVG(AvailabilityRate) AS DisponibilidadPromedio,
                    MIN(AvailabilityRate) AS DisponibilidadMinima,

                    SUM(
                        CASE
                            WHEN CpuMaxPercent >= 90 THEN 1
                            ELSE 0
                        END
                    ) AS DiasCpuCritica,

                    SUM(
                        CASE
                            WHEN MemoryUsedMaxPercent >= 90 THEN 1
                            ELSE 0
                        END
                    ) AS DiasMemoriaCritica
                FROM PeriodMetrics
                GROUP BY
                    TenantId,
                    SubscriptionId,
                    Computer
            )
            SELECT
                CustomerName,
                TenantId,
                SubscriptionId,
                SubscriptionName,
                ResourceGroup,
                Computer,
                Desde,
                Hasta,
                DiasConDatos,
                CpuPromedio,
                CpuMaximo,
                CpuP95,
                MemoriaPromedio,
                MemoriaMaxima,
                MemoriaP95,
                DisponibilidadPromedio,
                DisponibilidadMinima,
                DiasCpuCritica,
                DiasMemoriaCritica,

                CAST(
                    CASE
                        WHEN DisponibilidadPromedio IS NULL
                            THEN 'NoData'

                        WHEN DisponibilidadPromedio < 95
                          OR CpuMaximo >= 90
                          OR MemoriaMaxima >= 90
                            THEN 'Critical'

                        WHEN DisponibilidadPromedio < 99
                          OR CpuMaximo >= 80
                          OR MemoriaMaxima >= 80
                            THEN 'Warning'

                        ELSE 'Healthy'
                    END
                    AS varchar(20)
                ) AS VMStatus,

                CAST(
                    CASE
                        WHEN DisponibilidadPromedio IS NULL
                            THEN 'Sin datos suficientes en Azure Monitor.'

                        WHEN DisponibilidadPromedio < 95
                            THEN 'Disponibilidad baja. Revisar apagados, reinicios, agente AMA, conectividad o ventanas de mantenimiento.'

                        WHEN CpuMaximo >= 90
                          AND MemoriaMaxima >= 90
                            THEN 'CPU y memoria críticas. Evaluar cambio de tamaño y revisar procesos de alto consumo.'

                        WHEN CpuMaximo >= 90
                            THEN 'CPU crítica. Revisar procesos y evaluar aumento de capacidad.'

                        WHEN MemoriaMaxima >= 90
                            THEN 'Memoria crítica. Revisar procesos y evaluar aumento de RAM.'

                        WHEN CpuPromedio < 20
                          AND MemoriaPromedio < 40
                            THEN 'Bajo consumo promedio. Evaluar rightsizing a una SKU menor.'

                        ELSE 'Sin recomendación crítica para el período seleccionado.'
                    END
                    AS varchar(250)
                ) AS Recomendacion
            FROM Aggregated
            ORDER BY
                CASE
                    WHEN DisponibilidadPromedio < 95
                      OR CpuMaximo >= 90
                      OR MemoriaMaxima >= 90
                        THEN 1

                    WHEN DisponibilidadPromedio < 99
                      OR CpuMaximo >= 80
                      OR MemoriaMaxima >= 80
                        THEN 2

                    ELSE 3
                END,
                Computer;
            """;

        using var connection =
            _connectionFactory.CreateConnection();

        connection.Open();

        var rows =
            await connection.QueryAsync<ClientVmMetricModel>(
                sql,
                new
                {
                    SubscriptionId = subscriptionId.Trim(),
                    Days = days
                });

        return rows.ToList();
    }

    public static ClientVmMetricsSummary BuildSummary(
        IReadOnlyCollection<ClientVmMetricModel> rows)
    {
        if (rows.Count == 0)
        {
            return new ClientVmMetricsSummary();
        }

        return new ClientVmMetricsSummary
        {
            TotalVMs = rows.Count,

            CpuPromedio = AverageNullable(
                rows.Select(row => row.CpuPromedio)),

            MemoriaPromedio = AverageNullable(
                rows.Select(row => row.MemoriaPromedio)),

            DisponibilidadPromedio = AverageNullable(
                rows.Select(row => row.DisponibilidadPromedio)),

            ConRecomendacion =
                rows.Count(row => row.TieneRecomendacion),

            SinDatos =
                rows.Count(row => row.SinDatos),

            Healthy =
                rows.Count(row =>
                    row.VMStatus.Equals(
                        "Healthy",
                        StringComparison.OrdinalIgnoreCase)),

            Warning =
                rows.Count(row =>
                    row.VMStatus.Equals(
                        "Warning",
                        StringComparison.OrdinalIgnoreCase)),

            Critical =
                rows.Count(row =>
                    row.VMStatus.Equals(
                        "Critical",
                        StringComparison.OrdinalIgnoreCase))
        };
    }

    private static int NormalizeDays(int days)
    {
        return days switch
        {
            7 => 7,
            15 => 15,
            30 => 30,
            _ => 7
        };
    }

    private static decimal? AverageNullable(
        IEnumerable<decimal?> values)
    {
        var available =
            values
                .Where(value => value.HasValue)
                .Select(value => value!.Value)
                .ToList();

        return available.Count == 0
            ? null
            : available.Average();
    }
}
