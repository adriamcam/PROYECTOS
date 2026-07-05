namespace ITQS.SupportOperationsCenter.Components.Reporting.HybridBenefit.Models;

public class HybridBenefitResourceHistory
{
    public string ResourceKey { get; set; } = "";
    public string ResourceName { get; set; } = "";
    public string Customer { get; set; } = "";
    public string ChangeType { get; set; } = "";
    public string Severity { get; set; } = "";
    public string OldValue { get; set; } = "";
    public string NewValue { get; set; } = "";
    public string ChangedFields { get; set; } = "";
    public DateTime ChangeDate { get; set; }
}
