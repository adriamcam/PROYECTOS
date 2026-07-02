namespace ITQS.SupportOperationsCenter.Models.Administration.AppRegistrations;

public sealed class AppRegistrationAssignableUserModel
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string BaseRole { get; set; } = string.Empty;
    public string EffectiveRole { get; set; } = string.Empty;
}
