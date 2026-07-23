using System.Data;
using System.Globalization;
using System.Text.Json;
using ClosedXML.Excel;
using Dapper;
using ITQS.SupportOperationsCenter.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Tabs;

public partial class ClientVirtualMachines
{
    [Inject]
    protected ISqlConnectionFactory ExcelConnectionFactory { get; set; }
        = default!;

    [Inject]
    protected IJSRuntime ExcelJsRuntime { get; set; }
        = default!;

    private bool IsExportingExcel { get; set; }

    protected async Task ExportToExcelAsync()
    {
        if (IsExportingExcel ||
            string.IsNullOrWhiteSpace(SubscriptionId))
        {
            return;
        }

        try
        {
            IsExportingExcel = true;
            StateHasChanged();

            const string vmSql = """
                SELECT *
                FROM dbo.VMInventoryCurrent
                WHERE IsActive = 1
                  AND TRY_CONVERT(
                        uniqueidentifier,
                        SubscriptionId
                      ) = TRY_CONVERT(
                        uniqueidentifier,
                        @SubscriptionId
                      )
                ORDER BY VMName;
                """;

            const string diskSql = """
                SELECT *
                FROM dbo.VMDiskInventoryCurrent
                WHERE TRY_CONVERT(
                        uniqueidentifier,
                        SubscriptionId
                      ) = TRY_CONVERT(
                        uniqueidentifier,
                        @SubscriptionId
                      )
                ORDER BY
                    VMName,
                    CASE
                        WHEN DiskRole = 'OS' THEN 0
                        WHEN DiskRole = 'Data' THEN 1
                        WHEN DiskRole = 'Local' THEN 2
                        ELSE 3
                    END,
                    LUN,
                    DiskName;
                """;

            using var connection =
                ExcelConnectionFactory.CreateConnection();

            connection.Open();

            var parameters = new
            {
                SubscriptionId = SubscriptionId.Trim()
            };

            var vmRows =
                (await connection.QueryAsync(
                    vmSql,
                    parameters))
                .Select(ToDictionary)
                .ToList();

            var diskRows =
                (await connection.QueryAsync(
                    diskSql,
                    parameters))
                .Select(ToDictionary)
                .ToList();

            using var workbook = new XLWorkbook();

            CreateVmWorksheet(workbook, vmRows);
            CreateDiskWorksheet(workbook, diskRows);
            CreateExtensionsWorksheet(workbook, vmRows);
            CreateTagsWorksheet(workbook, vmRows);

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            var base64 =
                Convert.ToBase64String(stream.ToArray());

            var customer =
                SanitizeFileName(
                    string.IsNullOrWhiteSpace(CustomerName)
                        ? "Cliente"
                        : CustomerName);

            var fileName =
                $"Inventario-VM-{customer}-{DateTime.Now:yyyyMMdd-HHmm}.xlsx";

            await ExcelJsRuntime.InvokeVoidAsync(
                "itqsDownloadBase64File",
                fileName,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                base64);
        }
        catch (Exception ex)
        {
            ErrorMessage =
                $"No fue posible exportar el inventario: {ex.Message}";
        }
        finally
        {
            IsExportingExcel = false;
            StateHasChanged();
        }
    }

    private static void CreateVmWorksheet(
        XLWorkbook workbook,
        IReadOnlyCollection<Dictionary<string, object?>> rows)
    {
        var worksheet =
            workbook.Worksheets.Add("Resumen VM");

        var headers = new[]
        {
            "Cliente",
            "Tenant ID",
            "Suscripción ID",
            "Suscripción",
            "Resource Group",
            "Nombre VM",
            "Computer Name",
            "Estado",
            "Provisioning State",
            "Operational Status",
            "Health Status",
            "Región",
            "Availability Zone",
            "VM Size",
            "VM Family",
            "vCPU",
            "Memoria GB",
            "Sistema Operativo",
            "VM Generation",
            "VM Architecture",
            "Source Image Publisher",
            "Source Image Offer",
            "Source Image SKU",
            "Source Image Version",
            "Source Image Plan",
            "Image Reference",
            "IP Pública",
            "Cantidad NIC",
            "OS Disk Name",
            "OS Disk Type",
            "OS Disk Size GB",
            "Data Disk Count",
            "Total Data Disk GB",
            "Total Storage GB",
            "Azure Monitor",
            "AMA Installed",
            "Dependency Agent",
            "Log Analytics Workspace",
            "Insights Enabled",
            "Defender Enabled",
            "Encryption Enabled",
            "Trusted Launch",
            "Secure Boot",
            "vTPM",
            "NSG Protected",
            "JIT Enabled",
            "Azure VM Agent",
            "Azure VM Agent Version",
            "Extensions Count",
            "Extensions",
            "Owner",
            "Environment",
            "Cost Center",
            "Business Unit",
            "Application",
            "Criticality",
            "Fecha Creación",
            "Último Boot",
            "Última Vez Vista",
            "Fecha Actualización"
        };

        WriteHeaders(worksheet, headers);

        var rowNumber = 2;

        foreach (var row in rows)
        {
            var osSize =
                GetDecimal(row, "OSDiskSizeGB");

            var dataSize =
                GetDecimal(row, "TotalDataDiskGB");

            var totalStorage =
                osSize + dataSize;

            var values = new object?[]
            {
                Get(row, "CustomerName"),
                Get(row, "TenantId"),
                Get(row, "SubscriptionId"),
                Get(row, "SubscriptionName"),
                Get(row, "ResourceGroupName"),
                Get(row, "VMName"),
                Get(row, "ComputerName"),
                Get(row, "PowerState"),
                Get(row, "ProvisioningState"),
                Get(row, "OperationalStatus"),
                Get(row, "HealthStatus"),
                Get(row, "Location"),
                Get(row, "AvailabilityZone"),
                Get(row, "VMSize"),
                Get(row, "VMFamily"),
                Get(row, "VCPUs"),
                Get(row, "MemoryGB"),
                Get(row, "OSType"),
                Get(row, "Generation", "VMGeneration"),
                Get(
                    row,
                    "VMArchitecture",
                    "Architecture",
                    "CpuArchitecture"),
                Get(row, "Publisher"),
                Get(row, "Offer"),
                Get(row, "ImageSku", "Sku"),
                Get(row, "Version"),
                Get(
                    row,
                    "SourceImagePlan",
                    "ImagePlan",
                    "PlanName"),
                Get(row, "ImageReference"),
                Get(row, "PublicIP"),
                Get(row, "NICCount"),
                Get(row, "OSDiskName"),
                Get(row, "OSDiskType"),
                Get(row, "OSDiskSizeGB"),
                Get(row, "DataDiskCount"),
                Get(row, "TotalDataDiskGB"),
                totalStorage,
                Get(row, "AzureMonitorEnabled"),
                Get(row, "AMAInstalled"),
                Get(row, "DependencyAgent"),
                Get(row, "LogAnalyticsWorkspace"),
                Get(row, "InsightsEnabled"),
                Get(row, "DefenderEnabled"),
                Get(row, "EncryptionEnabled"),
                Get(row, "TrustedLaunch"),
                Get(row, "SecureBoot"),
                Get(row, "VTpm"),
                Get(row, "NSGProtected"),
                Get(row, "JustInTimeEnabled"),
                Get(row, "AzureVMAgentProvisioned"),
                Get(row, "AzureVMAgentVersion"),
                Get(row, "ExtensionsCount"),
                Get(row, "ExtensionNames"),
                Get(row, "Owner"),
                Get(row, "Environment"),
                Get(row, "CostCenter"),
                Get(row, "BusinessUnit"),
                Get(row, "Application"),
                Get(row, "Criticality"),
                Get(row, "TimeCreated"),
                Get(row, "LastBootTime"),
                Get(row, "LastSeenAt"),
                Get(row, "UpdatedAt")
            };

            WriteRow(
                worksheet,
                rowNumber,
                values);

            rowNumber++;
        }

        FormatWorksheet(
            worksheet,
            headers.Length,
            rowNumber - 1);
    }

    private static void CreateDiskWorksheet(
        XLWorkbook workbook,
        IReadOnlyCollection<Dictionary<string, object?>> rows)
    {
        var worksheet =
            workbook.Worksheets.Add("Discos");

        var headers = new[]
        {
            "Cliente",
            "Tenant ID",
            "Suscripción ID",
            "Suscripción",
            "Resource Group",
            "Nombre VM",
            "Computer Name",
            "Región",
            "Tipo Disco",
            "Nombre Disco",
            "Tamaño GB",
            "Tier",
            "Disk Type",
            "LUN",
            "Caching",
            "IOPS",
            "Throughput MB/s",
            "Encryption Enabled",
            "Encryption Type",
            "Disk State",
            "Provisioning State",
            "Availability Zone",
            "Delete Option",
            "Shared Disk",
            "Max Shares",
            "Disk Resource ID",
            "VM Resource ID",
            "Última Vez Vista",
            "Fecha Actualización"
        };

        WriteHeaders(worksheet, headers);

        var rowNumber = 2;

        foreach (var row in rows)
        {
            var tier = Get(
                row,
                "ManagedDiskType",
                "StorageAccountType",
                "DiskType");

            var values = new object?[]
            {
                Get(row, "CustomerName"),
                Get(row, "TenantId"),
                Get(row, "SubscriptionId"),
                Get(row, "SubscriptionName"),
                Get(row, "ResourceGroupName"),
                Get(row, "VMName"),
                Get(row, "ComputerName"),
                Get(row, "Location"),
                Get(row, "DiskRole"),
                Get(row, "DiskName"),
                Get(row, "DiskSizeGB"),
                FormatDiskTier(tier),
                Get(row, "DiskType"),
                Get(row, "LUN"),
                Get(row, "Caching"),
                Get(row, "IOPSReadWrite"),
                Get(row, "ThroughputMBpsReadWrite"),
                Get(row, "EncryptionEnabled"),
                Get(row, "EncryptionType"),
                Get(row, "DiskState"),
                Get(row, "ProvisioningState"),
                Get(row, "AvailabilityZone"),
                Get(row, "DeleteOption"),
                Get(row, "IsSharedDisk"),
                Get(row, "MaxShares"),
                Get(row, "DiskResourceId"),
                Get(row, "VMResourceId"),
                Get(row, "LastSeenAt"),
                Get(row, "UpdatedAt")
            };

            WriteRow(
                worksheet,
                rowNumber,
                values);

            rowNumber++;
        }

        FormatWorksheet(
            worksheet,
            headers.Length,
            rowNumber - 1);
    }

    private static void CreateExtensionsWorksheet(
        XLWorkbook workbook,
        IReadOnlyCollection<Dictionary<string, object?>> rows)
    {
        var worksheet =
            workbook.Worksheets.Add("Extensiones");

        var headers = new[]
        {
            "Cliente",
            "Suscripción",
            "Resource Group",
            "Nombre VM",
            "Computer Name",
            "Extensión"
        };

        WriteHeaders(worksheet, headers);

        var rowNumber = 2;

        foreach (var row in rows)
        {
            var extensions =
                Get(row, "ExtensionNames")?
                    .ToString()?
                    .Split(
                        ';',
                        StringSplitOptions.RemoveEmptyEntries |
                        StringSplitOptions.TrimEntries)
                ?? [];

            foreach (var extension in extensions)
            {
                WriteRow(
                    worksheet,
                    rowNumber,
                    new object?[]
                    {
                        Get(row, "CustomerName"),
                        Get(row, "SubscriptionName"),
                        Get(row, "ResourceGroupName"),
                        Get(row, "VMName"),
                        Get(row, "ComputerName"),
                        extension
                    });

                rowNumber++;
            }
        }

        FormatWorksheet(
            worksheet,
            headers.Length,
            rowNumber - 1);
    }

    private static void CreateTagsWorksheet(
        XLWorkbook workbook,
        IReadOnlyCollection<Dictionary<string, object?>> rows)
    {
        var worksheet =
            workbook.Worksheets.Add("Tags");

        var headers = new[]
        {
            "Cliente",
            "Suscripción",
            "Resource Group",
            "Nombre VM",
            "Tag",
            "Valor"
        };

        WriteHeaders(worksheet, headers);

        var rowNumber = 2;

        foreach (var row in rows)
        {
            var tagsJson =
                Get(row, "Tags")?.ToString();

            if (string.IsNullOrWhiteSpace(tagsJson))
            {
                continue;
            }

            try
            {
                using var document =
                    JsonDocument.Parse(tagsJson);

                if (document.RootElement.ValueKind !=
                    JsonValueKind.Object)
                {
                    continue;
                }

                foreach (var tag in
                         document.RootElement.EnumerateObject())
                {
                    WriteRow(
                        worksheet,
                        rowNumber,
                        new object?[]
                        {
                            Get(row, "CustomerName"),
                            Get(row, "SubscriptionName"),
                            Get(row, "ResourceGroupName"),
                            Get(row, "VMName"),
                            tag.Name,
                            tag.Value.ToString()
                        });

                    rowNumber++;
                }
            }
            catch (JsonException)
            {
                WriteRow(
                    worksheet,
                    rowNumber,
                    new object?[]
                    {
                        Get(row, "CustomerName"),
                        Get(row, "SubscriptionName"),
                        Get(row, "ResourceGroupName"),
                        Get(row, "VMName"),
                        "Tags",
                        tagsJson
                    });

                rowNumber++;
            }
        }

        FormatWorksheet(
            worksheet,
            headers.Length,
            rowNumber - 1);
    }

    private static Dictionary<string, object?> ToDictionary(
        dynamic row)
    {
        var source =
            (IDictionary<string, object>)row;

        return source.ToDictionary(
            item => item.Key,
            item => item.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    private static object? Get(
        IReadOnlyDictionary<string, object?> row,
        params string[] names)
    {
        foreach (var name in names)
        {
            if (row.TryGetValue(
                    name,
                    out var value) &&
                value is not DBNull)
            {
                return value;
            }
        }

        return null;
    }

    private static decimal GetDecimal(
        IReadOnlyDictionary<string, object?> row,
        params string[] names)
    {
        var value = Get(row, names);

        if (value is null)
        {
            return 0;
        }

        return decimal.TryParse(
            Convert.ToString(
                value,
                CultureInfo.InvariantCulture),
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out var result)
            ? result
            : 0;
    }

    private static void WriteHeaders(
        IXLWorksheet worksheet,
        IReadOnlyList<string> headers)
    {
        for (var column = 0;
             column < headers.Count;
             column++)
        {
            worksheet.Cell(
                    1,
                    column + 1)
                .Value = headers[column];
        }
    }

    private static void WriteRow(
        IXLWorksheet worksheet,
        int rowNumber,
        IReadOnlyList<object?> values)
    {
        for (var column = 0;
             column < values.Count;
             column++)
        {
            SetCellValue(
                worksheet.Cell(
                    rowNumber,
                    column + 1),
                values[column]);
        }
    }

    private static void SetCellValue(
        IXLCell cell,
        object? value)
    {
        switch (value)
        {
            case null:
            case DBNull:
                cell.Value = string.Empty;
                break;

            case bool boolean:
                cell.Value = boolean;
                break;

            case byte byteValue:
                cell.Value = byteValue;
                break;

            case short shortValue:
                cell.Value = shortValue;
                break;

            case int intValue:
                cell.Value = intValue;
                break;

            case long longValue:
                cell.Value = longValue;
                break;

            case float floatValue:
                cell.Value = floatValue;
                break;

            case double doubleValue:
                cell.Value = doubleValue;
                break;

            case decimal decimalValue:
                cell.Value = decimalValue;
                break;

            case DateTime dateTime:
                cell.Value = dateTime;
                cell.Style.DateFormat.Format =
                    "yyyy-MM-dd HH:mm:ss";
                break;

            case DateTimeOffset dateTimeOffset:
                cell.Value = dateTimeOffset.DateTime;
                cell.Style.DateFormat.Format =
                    "yyyy-MM-dd HH:mm:ss";
                break;

            case Guid guid:
                cell.Value = guid.ToString();
                break;

            default:
                cell.Value =
                    Convert.ToString(
                        value,
                        CultureInfo.InvariantCulture)
                    ?? string.Empty;
                break;
        }
    }

    private static void FormatWorksheet(
        IXLWorksheet worksheet,
        int columnCount,
        int lastRow)
    {
        var header =
            worksheet.Range(
                1,
                1,
                1,
                columnCount);

        header.Style.Font.Bold = true;
        header.Style.Alignment.Vertical =
            XLAlignmentVerticalValues.Center;

        header.Style.Fill.BackgroundColor =
            XLColor.FromHtml("#EAF2FF");

        header.Style.Border.BottomBorder =
            XLBorderStyleValues.Thin;

        worksheet.SheetView.FreezeRows(1);

        if (lastRow > 1)
        {
            worksheet.Range(
                1,
                1,
                lastRow,
                columnCount)
            .CreateTable(
                "tbl_" + Guid.NewGuid().ToString("N"));
        }

        worksheet.Columns().AdjustToContents(
            5,
            60);

        worksheet.Rows().Style.Alignment.Vertical =
            XLAlignmentVerticalValues.Top;


    }

    private static string FormatDiskTier(
        object? tierValue)
    {
        var tier =
            tierValue?.ToString() ?? string.Empty;

        return tier switch
        {
            "Premium_LRS" => "Premium SSD",
            "PremiumV2_LRS" => "Premium SSD v2",
            "StandardSSD_LRS" => "Standard SSD",
            "Standard_LRS" => "Standard HDD",
            "UltraSSD_LRS" => "Ultra Disk",
            "LocalTemporary" => "Local temporal",
            _ => tier
        };
    }

    private static string SanitizeFileName(
        string value)
    {
        foreach (var invalidCharacter in
                 Path.GetInvalidFileNameChars())
        {
            value =
                value.Replace(
                    invalidCharacter,
                    '-');
        }

        return value.Trim();
    }
}

