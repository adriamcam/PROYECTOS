namespace ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Models.Metrics;

public sealed class ClientVmMetricsSummary
{
    public int TotalVMs { get; set; }

    public decimal? CpuPromedio { get; set; }

    public decimal? MemoriaPromedio { get; set; }

    public decimal? DisponibilidadPromedio { get; set; }

    public int ConRecomendacion { get; set; }

    public int SinDatos { get; set; }

    public int Healthy { get; set; }

    public int Warning { get; set; }

    public int Critical { get; set; }
}
