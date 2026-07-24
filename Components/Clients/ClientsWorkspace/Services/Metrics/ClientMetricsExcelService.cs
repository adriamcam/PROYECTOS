using ClosedXML.Excel;
using ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Models.Metrics;

namespace ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Services.Metrics;

public sealed class ClientMetricsExcelService
{
    public byte[] GenerateVmMetricsWorkbook(
        string customerName,
        string subscriptionName,
        int days,
        ClientVmMetricsSummary summary,
        IReadOnlyCollection<ClientVmMetricModel> metrics)
    {
        using var workbook = new XLWorkbook();

        CreateSummarySheet(
            workbook,
            customerName,
            subscriptionName,
            days,
            summary);

        CreateDetailSheet(
            workbook,
            metrics);

        using var stream = new MemoryStream();

        workbook.SaveAs(stream);

        return stream.ToArray();
    }

    private static void CreateSummarySheet(
        XLWorkbook workbook,
        string customerName,
        string subscriptionName,
        int days,
        ClientVmMetricsSummary summary)
    {
        var worksheet =
            workbook.Worksheets.Add("Resumen");

        worksheet.Cell("A1").Value =
            "Métricas de máquinas virtuales";

        worksheet.Range("A1:D1").Merge();

        worksheet.Cell("A3").Value = "Cliente";
        worksheet.Cell("B3").Value = customerName;

        worksheet.Cell("A4").Value = "Suscripción";
        worksheet.Cell("B4").Value = subscriptionName;

        worksheet.Cell("A5").Value = "Período";
        worksheet.Cell("B5").Value = $"{days} días";

        worksheet.Cell("A6").Value = "Fecha de generación";
        worksheet.Cell("B6").Value = DateTime.Now;

        worksheet.Cell("A8").Value = "Indicador";
        worksheet.Cell("B8").Value = "Valor";

        worksheet.Cell("A9").Value = "Total de máquinas virtuales";
        worksheet.Cell("B9").Value = summary.TotalVMs;

        worksheet.Cell("A10").Value = "CPU promedio";
        SetNullablePercentage(
            worksheet.Cell("B10"),
            summary.CpuPromedio);

        worksheet.Cell("A11").Value = "RAM promedio";
        SetNullablePercentage(
            worksheet.Cell("B11"),
            summary.MemoriaPromedio);

        worksheet.Cell("A12").Value = "Disponibilidad promedio";
        SetNullablePercentage(
            worksheet.Cell("B12"),
            summary.DisponibilidadPromedio);

        worksheet.Cell("A13").Value = "Con recomendación";
        worksheet.Cell("B13").Value = summary.ConRecomendacion;

        worksheet.Cell("A14").Value = "Sin datos";
        worksheet.Cell("B14").Value = summary.SinDatos;

        worksheet.Cell("A15").Value = "Healthy";
        worksheet.Cell("B15").Value = summary.Healthy;

        worksheet.Cell("A16").Value = "Warning";
        worksheet.Cell("B16").Value = summary.Warning;

        worksheet.Cell("A17").Value = "Critical";
        worksheet.Cell("B17").Value = summary.Critical;

        worksheet.Cell("A18").Value = "No Data";
        worksheet.Cell("B18").Value = summary.NoData;

        worksheet.Range("A1:D1").Style.Font.Bold = true;
        worksheet.Range("A8:B8").Style.Font.Bold = true;

        worksheet.Column("A").Width = 30;
        worksheet.Column("B").Width = 45;
        worksheet.Column("C").Width = 18;
        worksheet.Column("D").Width = 18;

        worksheet.SheetView.FreezeRows(1);
    }

    private static void CreateDetailSheet(
        XLWorkbook workbook,
        IReadOnlyCollection<ClientVmMetricModel> metrics)
    {
        var worksheet =
            workbook.Worksheets.Add("Detalle");

        var headers = new[]
        {
            "Cliente",
            "Suscripción",
            "Resource Group",
            "Máquina virtual",
            "Desde",
            "Hasta",
            "Días con datos",
            "CPU promedio",
            "CPU máxima",
            "CPU P95",
            "RAM promedio",
            "RAM máxima",
            "RAM P95",
            "Disponibilidad promedio",
            "Disponibilidad mínima",
            "Días CPU crítica",
            "Días memoria crítica",
            "Estado",
            "Recomendación"
        };

        for (var column = 0; column < headers.Length; column++)
        {
            worksheet.Cell(1, column + 1).Value =
                headers[column];
        }

        var rowNumber = 2;

        foreach (var metric in metrics)
        {
            worksheet.Cell(rowNumber, 1).Value =
                metric.CustomerName;

            worksheet.Cell(rowNumber, 2).Value =
                metric.SubscriptionName;

            worksheet.Cell(rowNumber, 3).Value =
                metric.ResourceGroup;

            worksheet.Cell(rowNumber, 4).Value =
                metric.Computer;

            if (metric.Desde.HasValue)
            {
                worksheet.Cell(rowNumber, 5).Value =
                    metric.Desde.Value;

                worksheet.Cell(rowNumber, 5)
                    .Style.DateFormat.Format = "yyyy-MM-dd";
            }

            if (metric.Hasta.HasValue)
            {
                worksheet.Cell(rowNumber, 6).Value =
                    metric.Hasta.Value;

                worksheet.Cell(rowNumber, 6)
                    .Style.DateFormat.Format = "yyyy-MM-dd";
            }

            worksheet.Cell(rowNumber, 7).Value =
                metric.DiasConDatos;

            SetNullablePercentage(
                worksheet.Cell(rowNumber, 8),
                metric.CpuPromedio);

            SetNullablePercentage(
                worksheet.Cell(rowNumber, 9),
                metric.CpuMaximo);

            SetNullablePercentage(
                worksheet.Cell(rowNumber, 10),
                metric.CpuP95);

            SetNullablePercentage(
                worksheet.Cell(rowNumber, 11),
                metric.MemoriaPromedio);

            SetNullablePercentage(
                worksheet.Cell(rowNumber, 12),
                metric.MemoriaMaxima);

            SetNullablePercentage(
                worksheet.Cell(rowNumber, 13),
                metric.MemoriaP95);

            SetNullablePercentage(
                worksheet.Cell(rowNumber, 14),
                metric.DisponibilidadPromedio);

            SetNullablePercentage(
                worksheet.Cell(rowNumber, 15),
                metric.DisponibilidadMinima);

            worksheet.Cell(rowNumber, 16).Value =
                metric.DiasCpuCritica;

            worksheet.Cell(rowNumber, 17).Value =
                metric.DiasMemoriaCritica;

            worksheet.Cell(rowNumber, 18).Value =
                metric.VMStatus;

            worksheet.Cell(rowNumber, 19).Value =
                metric.Recomendacion;

            rowNumber++;
        }

        worksheet.Range(
                1,
                1,
                1,
                headers.Length)
            .Style.Font.Bold = true;

        worksheet.SheetView.FreezeRows(1);

        worksheet.RangeUsed()?.SetAutoFilter();

        worksheet.Columns(1, 18).AdjustToContents();

        worksheet.Column(19).Width = 70;
        worksheet.Column(19).Style.Alignment.WrapText = true;
    }

    private static void SetNullablePercentage(
        IXLCell cell,
        decimal? value)
    {
        if (!value.HasValue)
        {
            return;
        }

        cell.Value = value.Value / 100m;
        cell.Style.NumberFormat.Format = "0.00%";
    }
}
