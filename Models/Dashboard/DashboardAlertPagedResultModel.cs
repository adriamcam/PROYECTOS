namespace ITQS.SupportOperationsCenter.Models.Dashboard;

public sealed class DashboardAlertPagedResultModel
{
    public List<DashboardAlertItemModel> Items { get; set; } = new();

    public int TotalCount { get; set; }

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 25;

    public int TotalPages =>
        PageSize == 0
            ? 0
            : (int)Math.Ceiling((double)TotalCount / PageSize);

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;
}