using Dapper;
using Microsoft.Data.SqlClient;
using ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Models;

namespace ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Services;

public sealed class AdvisorCatalogService : IAdvisorCatalogService
{
    private readonly IConfiguration _configuration;

    public AdvisorCatalogService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private SqlConnection CreateConnection()
    {
        var value = _configuration.GetConnectionString("ReportesDb");

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                "No se encontró ConnectionStrings:ReportesDb.");
        }

        return new SqlConnection(value);
    }

    public async Task<IReadOnlyList<AdvisorCatalogItem>> GetAllAsync()
    {
        const string sql = """
        SELECT
            Id,
            Recommendation,
            RecommendationSpanish,
            DescriptionSpanish,
            CostClassification,
            RequiresMaintenanceWindow,
            ImplementationMinutes,
            ImplementationHours,
            Category,
            Complexity,
            Notes,
            IsActive,
            UpdatedAt,
            RowVersion
        FROM dbo.AdvisorRecommendationCatalog
        ORDER BY
            IsActive DESC,
            Category,
            RecommendationSpanish;
        """;

        await using var connection = CreateConnection();
        var rows = await connection.QueryAsync<AdvisorCatalogItem>(sql);
        return rows.AsList();
    }

    public async Task<AdvisorCatalogSaveResult> UpdateAsync(
        AdvisorCatalogItem item)
    {
        const string sql = """
        UPDATE dbo.AdvisorRecommendationCatalog
        SET
            RecommendationSpanish = @RecommendationSpanish,
            DescriptionSpanish = @DescriptionSpanish,
            CostClassification = @CostClassification,
            RequiresMaintenanceWindow = @RequiresMaintenanceWindow,
            ImplementationMinutes = @ImplementationMinutes,
            ImplementationHours = @ImplementationHours,
            Category = @Category,
            Complexity = @Complexity,
            Notes = @Notes,
            IsActive = @IsActive,
            UpdatedAt = SYSUTCDATETIME()
        OUTPUT
            INSERTED.RowVersion,
            INSERTED.UpdatedAt
        WHERE Id = @Id
          AND RowVersion = @RowVersion;
        """;

        await using var connection = CreateConnection();

        var output = await connection.QuerySingleOrDefaultAsync<SaveOutput>(
            sql,
            new
            {
                item.Id,
                RecommendationSpanish = item.RecommendationSpanish.Trim(),
                DescriptionSpanish = NullIfEmpty(item.DescriptionSpanish),
                CostClassification = string.IsNullOrWhiteSpace(item.CostClassification)
                    ? "Por validar"
                    : item.CostClassification.Trim(),
                item.RequiresMaintenanceWindow,
                ImplementationMinutes = Math.Max(0, item.ImplementationMinutes),
                ImplementationHours = item.ImplementationHours is < 0
                    ? 0
                    : item.ImplementationHours,
                Category = NullIfEmpty(item.Category),
                Complexity = NullIfEmpty(item.Complexity),
                Notes = NullIfEmpty(item.Notes),
                item.IsActive,
                item.RowVersion
            });

        if (output is null)
        {
            return new AdvisorCatalogSaveResult
            {
                Success = false,
                Message =
                    "El registro cambió en la base de datos. " +
                    "Actualice la pantalla e inténtelo otra vez."
            };
        }

        return new AdvisorCatalogSaveResult
        {
            Success = true,
            Message = "Guardado.",
            RowVersion = output.RowVersion,
            UpdatedAt = output.UpdatedAt
        };
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed class SaveOutput
    {
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public DateTime UpdatedAt { get; set; }
    }
}
