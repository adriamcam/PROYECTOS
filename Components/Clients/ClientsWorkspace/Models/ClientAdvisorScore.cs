namespace ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Models;

public sealed class ClientAdvisorScore
{
    public string ScoreName { get; set; } = string.Empty;

    public decimal Score { get; set; }

    public decimal ConsumptionUnits { get; set; }

    public int ImpactedResourceCount { get; set; }

    public decimal PotentialScoreIncrease { get; set; }

    public DateTime? LastScoreDateUtc { get; set; }

    public DateTime? LastSeenUtc { get; set; }
}
