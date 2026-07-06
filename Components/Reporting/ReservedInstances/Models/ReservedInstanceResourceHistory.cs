namespace ITQS.SupportOperationsCenter.Components.Reporting.ReservedInstances.Models;

public sealed class ReservedInstanceResourceHistory
{
    public string ResourceKey { get; set; } = "";
    public string ChangeType { get; set; } = "";
    public string OldValue { get; set; } = "";
    public string NewValue { get; set; } = "";
    public DateTime ChangeDate { get; set; }
}
