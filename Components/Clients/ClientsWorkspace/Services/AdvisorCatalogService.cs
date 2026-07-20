using Dapper;
using System.Data;
using ITQS.SupportOperationsCenter.Data;
using ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Models;

namespace ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Services;

public sealed class AdvisorCatalogService : IAdvisorCatalogService
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public AdvisorCatalogService(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory
            ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    private IDbConnection CreateConnection() => _connectionFactory.CreateConnection();

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
        ORDER BY IsActive DESC, RecommendationSpanish, Recommendation;
        """;

        using var connection = CreateConnection();
        var rows = await connection.QueryAsync<AdvisorCatalogItem>(sql);
        return rows.AsList();
    }

    public async Task<AdvisorCatalogSaveResult> CreateAsync(AdvisorCatalogItem item)
    {
        const string sql = """
        IF EXISTS
        (
            SELECT 1
            FROM dbo.AdvisorRecommendationCatalog
            WHERE UPPER(LTRIM(RTRIM(Recommendation))) = UPPER(LTRIM(RTRIM(@Recommendation)))
        )
        BEGIN
            SELECT CAST(0 AS bit) AS Success,
                   CAST(0 AS int) AS Id,
                   CAST(NULL AS varbinary(8)) AS RowVersion,
                   CAST(NULL AS datetime2) AS UpdatedAt;
            RETURN;
        END;

        INSERT INTO dbo.AdvisorRecommendationCatalog
        (
            Recommendation,
            RecommendationSpanish,
            DescriptionSpanish,
            CostClassification,
            RequiresMaintenanceWindow,
            ImplementationMinutes,
            Category,
            Complexity,
            Notes,
            IsActive,
            UpdatedAt
        )
        OUTPUT
            CAST(1 AS bit) AS Success,
            INSERTED.Id,
            INSERTED.RowVersion,
            INSERTED.UpdatedAt
        VALUES
        (
            @Recommendation,
            @RecommendationSpanish,
            @DescriptionSpanish,
            @CostClassification,
            @RequiresMaintenanceWindow,
            @ImplementationMinutes,
            @Category,
            @Complexity,
            @Notes,
            @IsActive,
            SYSUTCDATETIME()
        );
        """;

        using var connection = CreateConnection();
        var output = await connection.QuerySingleAsync<SaveOutput>(sql, ToParameters(item));

        if (!output.Success)
        {
            return new AdvisorCatalogSaveResult
            {
                Success = false,
                Message = "Ya existe una recomendación con ese texto original."
            };
        }

        return new AdvisorCatalogSaveResult
        {
            Success = true,
            Message = "Recomendación creada.",
            Id = output.Id,
            RowVersion = output.RowVersion ?? Array.Empty<byte>(),
            UpdatedAt = output.UpdatedAt ?? DateTime.UtcNow
        };
    }

    public async Task<AdvisorCatalogSaveResult> UpdateAsync(AdvisorCatalogItem item)
    {
        // ImplementationHours no se modifica: en SQL es una columna calculada.
        const string sql = """
        UPDATE dbo.AdvisorRecommendationCatalog
        SET
            RecommendationSpanish = @RecommendationSpanish,
            DescriptionSpanish = @DescriptionSpanish,
            CostClassification = @CostClassification,
            RequiresMaintenanceWindow = @RequiresMaintenanceWindow,
            ImplementationMinutes = @ImplementationMinutes,
            Category = @Category,
            Complexity = @Complexity,
            Notes = @Notes,
            IsActive = @IsActive,
            UpdatedAt = SYSUTCDATETIME()
        OUTPUT INSERTED.RowVersion, INSERTED.UpdatedAt
        WHERE Id = @Id
          AND RowVersion = @RowVersion;
        """;

        using var connection = CreateConnection();
        var output = await connection.QuerySingleOrDefaultAsync<SaveOutput>(
            sql,
            new
            {
                item.Id,
                RecommendationSpanish = item.RecommendationSpanish.Trim(),
                DescriptionSpanish = NullIfEmpty(item.DescriptionSpanish),
                CostClassification = NormalizeCost(item.CostClassification),
                item.RequiresMaintenanceWindow,
                ImplementationMinutes = Math.Max(0, item.ImplementationMinutes),
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
                Message = "El registro cambió en la base de datos. Actualice la pantalla e inténtelo otra vez."
            };
        }

        return new AdvisorCatalogSaveResult
        {
            Success = true,
            Message = "Guardado.",
            Id = item.Id,
            RowVersion = output.RowVersion ?? Array.Empty<byte>(),
            UpdatedAt = output.UpdatedAt ?? DateTime.UtcNow
        };
    }

    public async Task<AdvisorCatalogImportPreview> PreviewImportAsync(
        IReadOnlyCollection<AdvisorCatalogImportRow> rows)
    {
        var previewRows = rows.Select(CloneImportRow).ToList();

        using var connection = CreateConnection();
        var existing = (await connection.QueryAsync<AdvisorCatalogItem>("""
            SELECT
                Id, Recommendation, RecommendationSpanish, DescriptionSpanish,
                CostClassification, RequiresMaintenanceWindow, ImplementationMinutes,
                ImplementationHours, Category, Complexity, Notes, IsActive,
                UpdatedAt, RowVersion
            FROM dbo.AdvisorRecommendationCatalog;
            """)).ToDictionary(
                x => NormalizeKey(x.Recommendation),
                x => x,
                StringComparer.OrdinalIgnoreCase);

        var duplicateKeys = previewRows
            .Where(x => !string.IsNullOrWhiteSpace(x.Recommendation))
            .GroupBy(x => NormalizeKey(x.Recommendation), StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var row in previewRows)
        {
            ValidateImportRow(row, duplicateKeys);

            if (!row.IsValid)
            {
                row.Action = "Error";
                continue;
            }

            if (!existing.TryGetValue(NormalizeKey(row.Recommendation), out var current))
            {
                row.Action = "Nuevo";
                continue;
            }

            row.Action = IsEquivalent(current, row) ? "Sin cambios" : "Actualizar";
        }

        return new AdvisorCatalogImportPreview { Rows = previewRows };
    }

    public async Task<AdvisorCatalogImportResult> ImportAsync(
        IReadOnlyCollection<AdvisorCatalogImportRow> rows)
    {
        var preview = await PreviewImportAsync(rows);
        var validChanges = preview.Rows
            .Where(x => x.IsValid && (x.Action == "Nuevo" || x.Action == "Actualizar"))
            .ToList();

        if (validChanges.Count == 0)
        {
            return new AdvisorCatalogImportResult
            {
                Success = preview.ErrorCount == 0,
                Message = "No hay cambios válidos para importar.",
                Unchanged = preview.UnchangedCount,
                Errors = preview.ErrorCount
            };
        }

        const string updateSql = """
        UPDATE dbo.AdvisorRecommendationCatalog
        SET
            RecommendationSpanish = @RecommendationSpanish,
            DescriptionSpanish = @DescriptionSpanish,
            CostClassification = @CostClassification,
            RequiresMaintenanceWindow = @RequiresMaintenanceWindow,
            ImplementationMinutes = @ImplementationMinutes,
            Category = @Category,
            Complexity = @Complexity,
            Notes = @Notes,
            IsActive = @IsActive,
            UpdatedAt = SYSUTCDATETIME()
        WHERE UPPER(LTRIM(RTRIM(Recommendation))) = UPPER(LTRIM(RTRIM(@Recommendation)));
        """;

        const string insertSql = """
        INSERT INTO dbo.AdvisorRecommendationCatalog
        (
            Recommendation, RecommendationSpanish, DescriptionSpanish,
            CostClassification, RequiresMaintenanceWindow,
            ImplementationMinutes, Category, Complexity, Notes,
            IsActive, UpdatedAt
        )
        VALUES
        (
            @Recommendation, @RecommendationSpanish, @DescriptionSpanish,
            @CostClassification, @RequiresMaintenanceWindow,
            @ImplementationMinutes, @Category, @Complexity, @Notes,
            @IsActive, SYSUTCDATETIME()
        );
        """;

        using var connection = CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var inserted = 0;
        var updated = 0;

        try
        {
            foreach (var row in validChanges)
            {
                var parameters = ToParameters(row);

                if (row.Action == "Nuevo")
                {
                    inserted += await connection.ExecuteAsync(insertSql, parameters, transaction);
                }
                else
                {
                    updated += await connection.ExecuteAsync(updateSql, parameters, transaction);
                }
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }

        return new AdvisorCatalogImportResult
        {
            Success = true,
            Message = "Importación completada.",
            Inserted = inserted,
            Updated = updated,
            Unchanged = preview.UnchangedCount,
            Errors = preview.ErrorCount
        };
    }

    public async Task<AdvisorCatalogDeleteResult> DeleteManyAsync(IReadOnlyCollection<int> ids)
    {
        var validIds = ids.Where(x => x > 0).Distinct().ToArray();

        if (validIds.Length == 0)
        {
            return new AdvisorCatalogDeleteResult
            {
                Success = false,
                Message = "No se seleccionaron recomendaciones."
            };
        }

        const string sql = """
        DELETE FROM dbo.AdvisorRecommendationCatalog
        WHERE Id IN @Ids;
        """;

        using var connection = CreateConnection();
        var deleted = await connection.ExecuteAsync(sql, new { Ids = validIds });

        return new AdvisorCatalogDeleteResult
        {
            Success = deleted > 0,
            Deleted = deleted,
            Message = deleted > 0
                ? $"Se eliminaron {deleted} recomendaciones."
                : "No se eliminaron registros."
        };
    }

    private static object ToParameters(AdvisorCatalogItem item) => new
    {
        Recommendation = item.Recommendation.Trim(),
        RecommendationSpanish = item.RecommendationSpanish.Trim(),
        DescriptionSpanish = NullIfEmpty(item.DescriptionSpanish),
        CostClassification = NormalizeCost(item.CostClassification),
        item.RequiresMaintenanceWindow,
        ImplementationMinutes = Math.Max(0, item.ImplementationMinutes),
        Category = NullIfEmpty(item.Category),
        Complexity = NullIfEmpty(item.Complexity),
        Notes = NullIfEmpty(item.Notes),
        item.IsActive
    };

    private static object ToParameters(AdvisorCatalogImportRow row) => new
    {
        Recommendation = row.Recommendation.Trim(),
        RecommendationSpanish = row.RecommendationSpanish.Trim(),
        DescriptionSpanish = NullIfEmpty(row.DescriptionSpanish),
        CostClassification = NormalizeCost(row.CostClassification),
        row.RequiresMaintenanceWindow,
        ImplementationMinutes = Math.Max(0, row.ImplementationMinutes),
        Category = NullIfEmpty(row.Category),
        Complexity = NullIfEmpty(row.Complexity),
        Notes = NullIfEmpty(row.Notes),
        row.IsActive
    };

    private static void ValidateImportRow(
        AdvisorCatalogImportRow row,
        ISet<string> duplicateKeys)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(row.Recommendation))
            errors.Add("Falta Recommendation");

        if (string.IsNullOrWhiteSpace(row.RecommendationSpanish))
            errors.Add("Falta RecommendationSpanish");

        if (row.ImplementationMinutes < 0)
            errors.Add("ImplementationMinutes no puede ser negativo");

        if (!string.IsNullOrWhiteSpace(row.Recommendation) &&
            duplicateKeys.Contains(NormalizeKey(row.Recommendation)))
        {
            errors.Add("Recommendation duplicada dentro del Excel");
        }

        row.ValidationMessage = string.Join("; ", errors);
    }

    private static bool IsEquivalent(
        AdvisorCatalogItem current,
        AdvisorCatalogImportRow incoming)
    {
        return Same(current.RecommendationSpanish, incoming.RecommendationSpanish)
            && Same(current.DescriptionSpanish, incoming.DescriptionSpanish)
            && Same(NormalizeCost(current.CostClassification), NormalizeCost(incoming.CostClassification))
            && current.RequiresMaintenanceWindow == incoming.RequiresMaintenanceWindow
            && current.ImplementationMinutes == Math.Max(0, incoming.ImplementationMinutes)
            && Same(current.Category, incoming.Category)
            && Same(current.Complexity, incoming.Complexity)
            && Same(current.Notes, incoming.Notes)
            && current.IsActive == incoming.IsActive;
    }

    private static AdvisorCatalogImportRow CloneImportRow(AdvisorCatalogImportRow row) => new()
    {
        ExcelRowNumber = row.ExcelRowNumber,
        Recommendation = row.Recommendation,
        RecommendationSpanish = row.RecommendationSpanish,
        DescriptionSpanish = row.DescriptionSpanish,
        CostClassification = row.CostClassification,
        RequiresMaintenanceWindow = row.RequiresMaintenanceWindow,
        ImplementationMinutes = row.ImplementationMinutes,
        Category = row.Category,
        Complexity = row.Complexity,
        Notes = row.Notes,
        IsActive = row.IsActive
    };

    private static bool Same(string? left, string? right) =>
        string.Equals(left?.Trim() ?? string.Empty,
                      right?.Trim() ?? string.Empty,
                      StringComparison.OrdinalIgnoreCase);

    private static string NormalizeKey(string value) => value.Trim();

    private static string NormalizeCost(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "Por validar" : value.Trim();

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed class SaveOutput
    {
        public bool Success { get; set; }
        public int Id { get; set; }
        public byte[]? RowVersion { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
