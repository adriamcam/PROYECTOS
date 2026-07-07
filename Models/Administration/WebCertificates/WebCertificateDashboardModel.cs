namespace ITQS.SupportOperationsCenter.Models.Administration.WebCertificates;

public sealed class WebCertificateDashboardModel
{
    public int TotalCertificates { get; set; }
    public int Expired { get; set; }
    public int Critical { get; set; }
    public int Warning { get; set; }
    public int Healthy { get; set; }
    public int Unknown { get; set; }
    public int AppGatewayCount { get; set; }
    public int AppServiceCount { get; set; }
    public int KeyVaultCount { get; set; }
    public DateTime? LastScanAt { get; set; }
}