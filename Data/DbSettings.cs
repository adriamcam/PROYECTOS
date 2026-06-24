namespace ITQS.SupportOperationsCenter.Data;

public sealed class DbSettings
{
    public const string SectionName = "ConnectionStrings";

    public string ReportesDB { get; set; } = string.Empty;
}
