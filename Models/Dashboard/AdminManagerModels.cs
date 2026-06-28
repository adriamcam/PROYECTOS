namespace ITQS.SupportOperationsCenter.Models.Dashboard;

public sealed class AdminManagerDashboardModel
{
    public int TotalAssignedGroups { get; set; }
    public int TotalAssignedEvents { get; set; }
    public int ActiveGroups { get; set; }
    public int InProgressGroups { get; set; }
    public int ClosedToday { get; set; }
    public int ClosedMonth { get; set; }
    public int UsersWithAlerts { get; set; }
    public int UnassignedCloseableGroups { get; set; }
    public int CriticalGroups { get; set; }
}

public sealed class AdminManagerAlertPagedResultModel
{
    public List<AdminManagerAlertItemModel> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 25;

    public int TotalPages =>
        PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed class AdminManagerAlertItemModel
{
    public bool Selected { get; set; }

    public string SourceType { get; set; } = string.Empty; // Management / Backup
    public long Id { get; set; }

    public string ClientName { get; set; } = string.Empty;
    public string AssignedTo { get; set; } = string.Empty;
    public string AssignedEmail { get; set; } = string.Empty;

    public string AlertName { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty;
    public string ResourceName { get; set; } = string.Empty;

    public int Events { get; set; }
    public DateTime? LastEventAt { get; set; }

    public string AlertStatus { get; set; } = "Activa";
}

public sealed class AdminManagerAppUserModel
{
    public int Id { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string EffectiveRole { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class AdminManagerEngineerWorkloadModel
{
    public string UserEmail { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    public int ActiveGroups { get; set; }
    public int InProgressGroups { get; set; }
    public int TotalEvents { get; set; }
    public int ClosedToday { get; set; }

    public int TotalGroups => ActiveGroups + InProgressGroups;
}

public sealed class AdminManagerSeveritySummaryModel
{
    public string SourceType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public int Groups { get; set; }
    public int Events { get; set; }
    public int CloseableGroups { get; set; }
    public int CloseableEvents { get; set; }
}

public sealed class AdminManagerClosedHistoryModel
{
    public long HistoryId { get; set; }
    public string KPIType { get; set; } = string.Empty;
    public long AlertId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string AlertName { get; set; } = string.Empty;
    public string ResourceName { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string ClosedBy { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public DateTime? ClosedAt { get; set; }
}

public sealed class AdminManagerReassignRequestModel
{
    public List<AdminManagerAlertItemModel> Alerts { get; set; } = new();

    public string NewAssignedTo { get; set; } = string.Empty;
    public string NewAssignedEmail { get; set; } = string.Empty;

    public string RequestedBy { get; set; } = string.Empty;
    public string RequestedByEmail { get; set; } = string.Empty;

    public string Comment { get; set; } = string.Empty;
}

public sealed class AdminManagerCloseSeverityRequestModel
{
    public string SourceType { get; set; } = "All";
    public string Severity { get; set; } = string.Empty;

    public string RequestedBy { get; set; } = string.Empty;
    public string RequestedByEmail { get; set; } = string.Empty;

    public string Comment { get; set; } = string.Empty;
}
