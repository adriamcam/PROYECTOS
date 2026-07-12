namespace ITQS.SupportOperationsCenter.Components.Reporting.ReservedInstances.Models;

public sealed class RICoverageKpi
{
    public long TotalVMs { get; set; }
    public long CoveredAligned { get; set; }
    public long CoveredCompatible { get; set; }
    public long ProbablyCoveredNoTag { get; set; }
    public long TaggedSkuChanged { get; set; }
    public long AmbiguousCoverage { get; set; }
    public long InsufficientCoverage { get; set; }
    public long TagIRVigenteSinReserva { get; set; }
    public long TagIRVencida { get; set; }
    public long TagIREliminada { get; set; }
    public long TagIRNoInterpretable { get; set; }
    public long Uncovered { get; set; }
    public decimal AverageCoverageScore { get; set; }
}
