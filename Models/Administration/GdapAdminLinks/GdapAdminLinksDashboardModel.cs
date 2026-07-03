namespace ITQS.SupportOperationsCenter.Models.Administration.GdapAdminLinks;

public sealed class GdapAdminLinksDashboardModel
{
    public int TotalCustomers { get; set; }
    public int ActiveGdap { get; set; }
    public int WithoutGdap { get; set; }
    public int ApprovalPending { get; set; }
    public int ExpiringIn30Days { get; set; }
    public int ExpiringIn15Days { get; set; }
    public int ExpiringIn7Days { get; set; }
    public int ExpiringIn5Days { get; set; }
    public int DisabledCustomers { get; set; }
    public int PendingEmails { get; set; }
    public int AutomationErrors { get; set; }
    public DateTime? LastExecutionDate { get; set; }
    public DateTime? LastUpdated { get; set; }

    public string LastExecutionText => LastExecutionDate.HasValue
        ? LastExecutionDate.Value.ToString("dd/MM/yyyy HH:mm")
        : "Sin ejecución";
}
