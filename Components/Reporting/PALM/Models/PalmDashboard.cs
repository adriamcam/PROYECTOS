namespace ITQS.SupportOperationsCenter.Components.Reporting.PALM.Models;

public sealed class PalmDashboard
{
    public long TotalCustomers { get; set; }

    public int TotalOK { get; set; }

    public int TotalNOK { get; set; }

    public int TotalRefreshed { get; set; }

    public int PartnerIdCorrect { get; set; }

    public int WithoutPartnerId { get; set; }

    public int DifferentPartnerId { get; set; }

    public int RequiresAction { get; set; }

    public int TotalVisibleSubscriptions { get; set; }

    public decimal CompliancePercent { get; set; }

    public DateTime? FirstScanDate { get; set; }

    public DateTime? LastScanDate { get; set; }

    public DateTime? LastUpdatedAt { get; set; }}
