using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ClosedXML.Excel;
using CsvHelper;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SmartTestDataGenerator.Application.Interfaces;

namespace SmartTestDataGenerator.Infrastructure.Services
{
    public class ExportService : IExportService
    {
        public ExportService()
        {
            // Set QuestPDF Community License
            try
            {
                QuestPDF.Settings.License = LicenseType.Community;
            }
            catch
            {
                // Already set or error, swallow
            }
        }

        public Task<byte[]> ExportToJsonAsync(Dictionary<string, List<Dictionary<string, object>>> data)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(data, options);
            return Task.FromResult(jsonBytes);
        }

        public Task<byte[]> ExportToXmlAsync(Dictionary<string, List<Dictionary<string, object>>> data)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine("<DataSet>");

            foreach (var table in data)
            {
                sb.AppendLine($"  <{table.Key}>");
                foreach (var row in table.Value)
                {
                    sb.AppendLine("    <Row>");
                    foreach (var col in row)
                    {
                        var valueStr = col.Value != null 
                            ? System.Security.SecurityElement.Escape(col.Value.ToString()!) 
                            : "";
                        sb.AppendLine($"      <{col.Key}>{valueStr}</{col.Key}>");
                    }
                    sb.AppendLine("    </Row>");
                }
                sb.AppendLine($"  </{table.Key}>");
            }

            sb.AppendLine("</DataSet>");
            return Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
        }

        public Task<byte[]> ExportToCsvAsync(Dictionary<string, List<Dictionary<string, object>>> data)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var table in data)
                    {
                        var entry = archive.CreateEntry($"{table.Key}.csv");
                        using (var entryStream = entry.Open())
                        using (var writer = new StreamWriter(entryStream, Encoding.UTF8))
                        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                        {
                            var firstRow = table.Value.FirstOrDefault();
                            if (firstRow != null)
                            {
                                // Write Header
                                foreach (var colName in firstRow.Keys)
                                {
                                    csv.WriteField(colName);
                                }
                                csv.NextRecord();

                                // Write Rows
                                foreach (var row in table.Value)
                                {
                                    foreach (var val in row.Values)
                                    {
                                        if (val is DateTime dt)
                                        {
                                            csv.WriteField(dt.ToString("yyyy-MM-dd HH:mm:ss"));
                                        }
                                        else
                                        {
                                            csv.WriteField(val?.ToString() ?? "");
                                        }
                                    }
                                    csv.NextRecord();
                                }
                            }
                        }
                    }
                }
                return Task.FromResult(memoryStream.ToArray());
            }
        }

        public Task<byte[]> ExportToExcelAsync(Dictionary<string, List<Dictionary<string, object>>> data)
        {
            using (var workbook = new XLWorkbook())
            {
                foreach (var table in data)
                {
                    var worksheet = workbook.Worksheets.Add(table.Key);
                    var firstRow = table.Value.FirstOrDefault();
                    if (firstRow != null)
                    {
                        // Write Headers
                        int colIdx = 1;
                        foreach (var colName in firstRow.Keys)
                        {
                            worksheet.Cell(1, colIdx++).Value = colName;
                        }

                        // Write Rows
                        int rowIdx = 2;
                        foreach (var row in table.Value)
                        {
                            colIdx = 1;
                            foreach (var val in row.Values)
                            {
                                if (val is int || val is long)
                                {
                                    worksheet.Cell(rowIdx, colIdx++).Value = Convert.ToInt64(val);
                                }
                                else if (val is decimal || val is double || val is float)
                                {
                                    worksheet.Cell(rowIdx, colIdx++).Value = Convert.ToDouble(val);
                                }
                                else if (val is bool b)
                                {
                                    worksheet.Cell(rowIdx, colIdx++).Value = b;
                                }
                                else if (val is DateTime dt)
                                {
                                    worksheet.Cell(rowIdx, colIdx++).Value = dt;
                                }
                                else
                                {
                                    worksheet.Cell(rowIdx, colIdx++).Value = val?.ToString() ?? "";
                                }
                            }
                            rowIdx++;
                        }

                        // Auto-style header row
                        var headerRow = worksheet.Row(1);
                        headerRow.Style.Font.Bold = true;
                        headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#4f46e5");
                        headerRow.Style.Font.FontColor = XLColor.White;

                        worksheet.Columns().AdjustToContents();
                    }
                }

                using (var ms = new MemoryStream())
                {
                    workbook.SaveAs(ms);
                    return Task.FromResult(ms.ToArray());
                }
            }
        }

        public Task<byte[]> ExportToSqlAsync(Dictionary<string, List<Dictionary<string, object>>> data, string dialect)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"-- Generated SQL INSERT Script for {dialect}");
            sb.AppendLine($"-- Generation Date: {DateTime.Now}");
            sb.AppendLine();

            foreach (var table in data)
            {
                string escapedTableName = EscapeSqlIdentifier(table.Key, dialect);
                
                sb.AppendLine($"-- Table: {table.Key} ({table.Value.Count} rows)");
                
                foreach (var row in table.Value)
                {
                    var colNames = string.Join(", ", row.Keys.Select(k => EscapeSqlIdentifier(k, dialect)));
                    var colValues = string.Join(", ", row.Values.Select(v => FormatSqlValue(v)));
                    
                    sb.AppendLine($"INSERT INTO {escapedTableName} ({colNames}) VALUES ({colValues});");
                }
                sb.AppendLine();
            }

            return Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
        }

        public Task<byte[]> ExportToPdfAsync(string templateName, Dictionary<string, List<Dictionary<string, object>>> data)
        {
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(11));

                    // Header
                    page.Header()
                        .Text("Smart Test Data Generator")
                        .SemiBold().FontSize(18).FontColor("#4f46e5");

                    // Content
                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(20);
                            
                            // Summary Section
                            column.Item().Text(text =>
                            {
                                text.Span("Şablon Raporu: ").Bold();
                                text.Span(templateName).SemiBold();
                            });
                            
                            column.Item().Text(text =>
                            {
                                text.Span("Üretim Tarihi: ").Bold();
                                text.Span(DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
                            });

                            column.Item().Text("Aşağıda her tablo için ilk 5 satırlık örnek veri önizlemesi sunulmuştur:").Italic().FontSize(10);

                            // Table previews
                            foreach (var table in data)
                            {
                                column.Item().Column(tableCol =>
                                {
                                    tableCol.Spacing(5);
                                    
                                    tableCol.Item().Text(table.Key).Bold().FontSize(14).Underline().FontColor("#10b981");

                                    var previewRows = table.Value.Take(5).ToList();
                                    if (previewRows.Any())
                                    {
                                        var headers = previewRows.First().Keys.ToList();
                                        
                                        tableCol.Item().Table(pdfTbl =>
                                        {
                                            // Define columns
                                            pdfTbl.ColumnsDefinition(columns =>
                                            {
                                                foreach (var h in headers)
                                                {
                                                    columns.RelativeColumn();
                                                }
                                            });

                                            // Draw headers
                                            pdfTbl.Header(header =>
                                            {
                                                foreach (var h in headers)
                                                {
                                                    header.Cell().Background("#4f46e5").Padding(5).Text(h).Bold().FontColor(Colors.White).FontSize(9);
                                                }
                                            });

                                            // Draw rows
                                            foreach (var row in previewRows)
                                            {
                                                foreach (var val in row.Values)
                                                {
                                                    pdfTbl.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(val?.ToString() ?? "NULL").FontSize(8);
                                                }
                                            }
                                        });
                                    }
                                    else
                                    {
                                        tableCol.Item().Text("Tabloda kayıt bulunmamaktadır.").Italic().FontSize(9);
                                    }
                                });
                            }
                        });

                    // Footer
                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Sayfa ");
                            x.CurrentPageNumber();
                        });
                });
            });

            using (var ms = new MemoryStream())
            {
                doc.GeneratePdf(ms);
                return Task.FromResult(ms.ToArray());
            }
        }

        private string EscapeSqlIdentifier(string name, string dialect)
        {
            switch (dialect)
            {
                case "SQLServer":
                    return $"[{name}]";
                case "MySQL":
                    return $"`{name}`";
                case "PostgreSQL":
                case "SQLite":
                default:
                    return $"\"{name}\"";
            }
        }

        private string FormatSqlValue(object? val)
        {
            if (val == null) return "NULL";
            
            if (val is string s)
            {
                return $"'{s.Replace("'", "''")}'";
            }
            if (val is DateTime dt)
            {
                return $"'{dt.ToString("yyyy-MM-dd HH:mm:ss")}'";
            }
            if (val is bool b)
            {
                return b ? "1" : "0";
            }
            if (val is decimal || val is double || val is float)
            {
                return Convert.ToDouble(val).ToString(CultureInfo.InvariantCulture);
            }
            if (val is int || val is long)
            {
                return val.ToString()!;
            }
            
            return $"'{val.ToString()!.Replace("'", "''")}'";
        }
    }
}
