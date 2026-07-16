using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using SmartTestDataGenerator.Application.Interfaces;
using SmartTestDataGenerator.Core.Entities;
using SmartTestDataGenerator.Core.Interfaces;

namespace SmartTestDataGenerator.Web.Controllers
{
    public class GeneratorController : Controller
    {
        private readonly ITemplateService _templateService;
        private readonly IDataGeneratorService _generatorService;
        private readonly IExportService _exportService;
        private readonly IRepository<GenerationHistory> _historyRepository;
        private readonly IRepository<RecentActivity> _activityRepository;
        private readonly IWebHostEnvironment _env;

        public GeneratorController(
            ITemplateService templateService,
            IDataGeneratorService generatorService,
            IExportService exportService,
            IRepository<GenerationHistory> historyRepository,
            IRepository<RecentActivity> activityRepository,
            IWebHostEnvironment env)
        {
            _templateService = templateService;
            _generatorService = generatorService;
            _exportService = exportService;
            _historyRepository = historyRepository;
            _activityRepository = activityRepository;
            _env = env;
        }

        // POST: Generator/Generate
        [HttpPost]
        public async Task<IActionResult> Generate(
            int templateId, 
            int recordMultiplier, 
            string language, 
            int? seed, 
            string exportFormat, 
            string sqlDialect)
        {
            var template = await _templateService.GetTemplateWithDetailsAsync(templateId);
            if (template == null)
            {
                return Json(new { success = false, message = "Şablon bulunamadı." });
            }

            try
            {
                // Measure speed
                var stopwatch = Stopwatch.StartNew();

                // If seed is empty, generate a random one
                int seedValue = seed ?? new Random().Next(1000, 999999);

                // 1. Generate relational datasets in-memory
                var generatedData = await _generatorService.GenerateDataAsync(template, seedValue, language, recordMultiplier);

                stopwatch.Stop();
                long elapsedMs = stopwatch.ElapsedMilliseconds;

                // 2. Export dataset to bytes
                byte[] fileBytes;
                string fileExt;
                string contentType;

                switch (exportFormat.ToUpper())
                {
                    case "JSON":
                        fileBytes = await _exportService.ExportToJsonAsync(generatedData);
                        fileExt = ".json";
                        contentType = "application/json";
                        break;
                    case "XML":
                        fileBytes = await _exportService.ExportToXmlAsync(generatedData);
                        fileExt = ".xml";
                        contentType = "application/xml";
                        break;
                    case "CSV":
                        fileBytes = await _exportService.ExportToCsvAsync(generatedData);
                        fileExt = ".zip"; // CSV is zipped because it contains multiple tables
                        contentType = "application/zip";
                        break;
                    case "EXCEL":
                        fileBytes = await _exportService.ExportToExcelAsync(generatedData);
                        fileExt = ".xlsx";
                        contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        break;
                    case "SQL":
                        fileBytes = await _exportService.ExportToSqlAsync(generatedData, sqlDialect);
                        fileExt = ".sql";
                        contentType = "text/plain";
                        break;
                    case "PDF":
                        fileBytes = await _exportService.ExportToPdfAsync(template.Name, generatedData);
                        fileExt = ".pdf";
                        contentType = "application/pdf";
                        break;
                    default:
                        return Json(new { success = false, message = "Geçersiz dışa aktarma formatı." });
                }

                // 3. Save physical file to wwwroot/exports/
                string exportsFolder = Path.Combine(_env.WebRootPath, "exports");
                if (!Directory.Exists(exportsFolder))
                {
                    Directory.CreateDirectory(exportsFolder);
                }

                string safeTemplateName = string.Concat(template.Name.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_").ToLower();
                string uniqueFileName = $"{safeTemplateName}_{DateTime.Now.Ticks}{fileExt}";
                string physicalPath = Path.Combine(exportsFolder, uniqueFileName);

                await System.IO.File.WriteAllBytesAsync(physicalPath, fileBytes);

                // Relative URL path
                string relativePath = $"/exports/{uniqueFileName}";

                // 4. Create History log
                int totalRecordsCount = generatedData.Values.Sum(rows => rows.Count);
                
                var history = new GenerationHistory
                {
                    TemplateId = template.Id,
                    TemplateName = template.Name,
                    TotalRecords = totalRecordsCount,
                    ExportFormat = exportFormat,
                    GenerationSpeedMs = elapsedMs,
                    FilePath = relativePath,
                    Seed = seedValue,
                    CreatedAt = DateTime.UtcNow
                };

                await _historyRepository.AddAsync(history);
                await _historyRepository.SaveChangesAsync();

                // 5. Add activity log
                string formatText = exportFormat;
                if (exportFormat == "SQL") formatText = $"{sqlDialect} SQL";
                var activity = new RecentActivity
                {
                    ActivityType = "Generate",
                    Details = $"'{template.Name}' şablonundan {totalRecordsCount:#,##0} satır veri {formatText} formatında üretildi. ({elapsedMs} ms)",
                    Timestamp = DateTime.UtcNow
                };

                await _activityRepository.AddAsync(activity);
                await _activityRepository.SaveChangesAsync();

                return Json(new { success = true, downloadUrl = relativePath });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Veri üretilirken bir hata meydana geldi: {ex.Message}" });
            }
        }
    }
}
