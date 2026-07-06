namespace ITQS.SupportOperationsCenter.Components.Reporting.ReservedInstances.Models;

public sealed class ReservedInstanceChangeHistory
{
    public long Id { get; set; }
    public string? ResourceKey { get; set; }
    public string? Customer { get; set; }
    public string? Subscription { get; set; }
    public string? ResourceName { get; set; }
    public string? ChangeType { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? ChangedBy { get; set; }
    public DateTime? ChangeDate { get; set; }
    public string? Comments { get; set; }
}
