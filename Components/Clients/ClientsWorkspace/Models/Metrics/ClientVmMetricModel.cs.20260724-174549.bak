namespace ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Models.Metrics;

public sealed class ClientVmMetricModel
{
    public string CustomerName { get; set; } = string.Empty;

    public Guid TenantId { get; set; }

    public Guid SubscriptionId { get; set; }

    public string SubscriptionName { get; set; } = string.Empty;

    public string ResourceGroup { get; set; } = string.Empty;

    public string Computer { get; set; } = string.Empty;

    public DateTime? Desde { get; set; }

    public DateTime? Hasta { get; set; }

    public int DiasConDatos { get; set; }

    public decimal? CpuPromedio { get; set; }

    public decimal? CpuMaximo { get; set; }

    public decimal? CpuP95 { get; set; }

    public decimal? MemoriaPromedio { get; set; }

    public decimal? MemoriaMaxima { get; set; }

    public decimal? MemoriaP95 { get; set; }

    public decimal? DisponibilidadPromedio { get; set; }

    public decimal? DisponibilidadMinima { get; set; }

    public int DiasCpuCritica { get; set; }

    public int DiasMemoriaCritica { get; set; }

    public string VMStatus { get; set; } = string.Empty;

    public string Recomendacion { get; set; } = string.Empty;

    public bool SinDatos =>
        DiasConDatos <= 0;

    public bool TieneRecomendacion =>
        !string.IsNullOrWhiteSpace(Recomendacion) &&
        !Recomendacion.Equals(
            "Sin recomendación",
            StringComparison.OrdinalIgnoreCase);
}
