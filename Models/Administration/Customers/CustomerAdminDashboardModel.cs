namespace ITQS.SupportOperationsCenter.Models.Administration.Customers;

public sealed class CustomerAdminDashboardModel
{
    public int TotalCustomers { get; set; }
    public int ActiveCustomers { get; set; }
    public int InactiveCustomers { get; set; }
    public string LastUpdateText { get; set; } = "Nunca";
}
