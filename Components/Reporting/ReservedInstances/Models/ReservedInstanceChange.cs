namespace ITQS.SupportOperationsCenter.Components.Reporting.ReservedInstances.Models;

public sealed class ReservedInstanceChange
{
    public string ResourceName { get; set; } = "";
    public string ChangeType { get; set; } = "";
    public DateTime? ChangeDate { get; set; }
}
