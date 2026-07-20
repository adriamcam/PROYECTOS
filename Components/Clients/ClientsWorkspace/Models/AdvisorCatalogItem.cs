namespace ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Models;

public sealed class AdvisorCatalogItem
{
    public int Id { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    public string RecommendationSpanish { get; set; } = string.Empty;
    public string? DescriptionSpanish { get; set; }
    public string CostClassification { get; set; } = "Por validar";
    public bool RequiresMaintenanceWindow { get; set; }
    public int ImplementationMinutes { get; set; }
    public decimal? ImplementationHours { get; set; }
    public string? Category { get; set; }
    public string? Complexity { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // Estado exclusivo de la interfaz.
    public bool IsEditing { get; set; }
    public bool IsSaving { get; set; }
    public string StatusMessage { get; set; } = string.Empty;

    public AdvisorCatalogItem Clone() => new()
    {
        Id = Id,
        Recommendation = Recommendation,
        RecommendationSpanish = RecommendationSpanish,
        DescriptionSpanish = DescriptionSpanish,
        CostClassification = CostClassification,
        RequiresMaintenanceWindow = RequiresMaintenanceWindow,
        ImplementationMinutes = ImplementationMinutes,
        ImplementationHours = ImplementationHours,
        Category = Category,
        Complexity = Complexity,
        Notes = Notes,
        IsActive = IsActive,
        UpdatedAt = UpdatedAt,
        RowVersion = RowVersion.ToArray()
    };
}

public sealed class AdvisorCatalogSaveResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
    public DateTime UpdatedAt { get; init; }
    public int Id { get; init; }
}

public sealed class AdvisorCatalogImportRow
{
    public int ExcelRowNumber { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    public string RecommendationSpanish { get; set; } = string.Empty;
    public string? DescriptionSpanish { get; set; }
    public string CostClassification { get; set; } = "Por validar";
    public bool RequiresMaintenanceWindow { get; set; }
    public int ImplementationMinutes { get; set; }
    public string? Category { get; set; }
    public string? Complexity { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public string Action { get; set; } = "Sin cambios";
    public string ValidationMessage { get; set; } = string.Empty;
    public bool IsValid => string.IsNullOrWhiteSpace(ValidationMessage);
}

public sealed class AdvisorCatalogImportPreview
{
    public List<AdvisorCatalogImportRow> Rows { get; init; } = new();
    public int NewCount => Rows.Count(x => x.Action == "Nuevo" && x.IsValid);
    public int UpdateCount => Rows.Count(x => x.Action == "Actualizar" && x.IsValid);
    public int UnchangedCount => Rows.Count(x => x.Action == "Sin cambios" && x.IsValid);
    public int ErrorCount => Rows.Count(x => !x.IsValid);
    public int ValidChangeCount => NewCount + UpdateCount;
}

public sealed class AdvisorCatalogImportResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int Inserted { get; init; }
    public int Updated { get; init; }
    public int Unchanged { get; init; }
    public int Errors { get; init; }
}

public sealed class AdvisorCatalogDeleteResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int Deleted { get; init; }
}
