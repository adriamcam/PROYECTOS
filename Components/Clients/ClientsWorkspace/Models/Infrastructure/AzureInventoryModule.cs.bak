namespace ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Models.Infrastructure;

public sealed class AzureInventoryModule
{
    public string Key { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    public string SourceName { get; init; } = string.Empty;
    public int ResourceCount { get; set; }
    public bool IsSpecialized { get; init; }

    public bool IsAvailable => ResourceCount > 0;
}
