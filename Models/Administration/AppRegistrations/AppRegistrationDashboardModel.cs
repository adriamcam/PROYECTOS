namespace ITQS.SupportOperationsCenter.Models.Administration.AppRegistrations;

public sealed class AppRegistrationDashboardModel
{
    public int TotalCustomers { get; set; }
    public int TotalAppRegistrations { get; set; }
    public int TotalSecrets { get; set; }
    public int TotalCertificates { get; set; }
    public int Healthy { get; set; }
    public int ExpireIn30Days { get; set; }
    public int ExpireIn15Days { get; set; }
    public int Expired { get; set; }
    public DateTime? LastScanDate { get; set; }

    public int TotalCredentials => TotalSecrets + TotalCertificates;

    public int HealthPercent =>
        TotalCredentials == 0
            ? 0
            : (int)Math.Round((Healthy / (double)TotalCredentials) * 100);

    public string LastScanText => LastScanDate.HasValue
        ? LastScanDate.Value.ToString("dd/MM/yyyy HH:mm")
        : "-";
}
