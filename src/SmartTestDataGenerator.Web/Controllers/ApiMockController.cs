using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SmartTestDataGenerator.Application.Interfaces;

namespace SmartTestDataGenerator.Web.Controllers
{
    public class ApiMockController : Controller
    {
        private readonly ITemplateService _templateService;
        private readonly IDataGeneratorService _generatorService;

        public ApiMockController(ITemplateService templateService, IDataGeneratorService generatorService)
        {
            _templateService = templateService;
            _generatorService = generatorService;
        }

        // GET: ApiMock/Documentation
        public async Task<IActionResult> Documentation()
        {
            ViewData["Title"] = "Mock API";
            ViewData["PageHeader"] = "REST Mock API Servisi";

            var templates = await _templateService.GetAllWithTablesAsync();
            return View(templates);
        }

        // GET: api/mock/{templateId}/{tableName}
        [HttpGet]
        [Route("api/mock/{templateId}/{tableName}")]
        public async Task<IActionResult> GetMockData(int templateId, string tableName, [FromQuery] int count = 10)
        {
            if (count <= 0) count = 10;
            if (count > 500) count = 500; // Cap to prevent resource exhaustion

            var template = await _templateService.GetTemplateWithDetailsAsync(templateId);
            if (template == null)
            {
                return NotFound(new { error = "Şablon bulunamadı." });
            }

            var table = template.Tables.FirstOrDefault(t => t.Name.Equals(tableName, StringComparison.OrdinalIgnoreCase));
            if (table == null)
            {
                return NotFound(new { error = $"Tablo '{tableName}' bu şablonda mevcut değil." });
            }

            try
            {
                // Temporarily override the table record count in the template to request count
                table.RecordCount = count;
                
                // Set multiplier to 1 to respect the count directly
                var generatedData = await _generatorService.GenerateDataAsync(template, null, "tr", 1);
                
                if (generatedData.TryGetValue(table.Name, out var rows))
                {
                    return Json(rows);
                }

                return NotFound(new { error = "Veri üretilemedi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Mock verisi üretilirken hata: {ex.Message}" });
            }
        }

        // POST: api/mock/{templateId}/{tableName}
        [HttpPost]
        [Route("api/mock/{templateId}/{tableName}")]
        public async Task<IActionResult> PostMockData(int templateId, string tableName, [FromBody] object? body)
        {
            var template = await _templateService.GetTemplateWithDetailsAsync(templateId);
            if (template == null)
            {
                return NotFound(new { error = "Şablon bulunamadı." });
            }

            var table = template.Tables.FirstOrDefault(t => t.Name.Equals(tableName, StringComparison.OrdinalIgnoreCase));
            if (table == null)
            {
                return NotFound(new { error = $"Tablo '{tableName}' bu şablonda mevcut değil." });
            }

            try
            {
                // Generate 1 row representing a newly created entity
                table.RecordCount = 1;
                var generatedData = await _generatorService.GenerateDataAsync(template, null, "tr", 1);

                if (generatedData.TryGetValue(table.Name, out var rows) && rows.Any())
                {
                    var responseItem = rows.First();
                    // If user sent a body, we can merge or show it back
                    return Created($"/api/mock/{templateId}/{tableName}/{responseItem["Id"]}", new
                    {
                        success = true,
                        message = "Kayıt başarıyla simüle edildi.",
                        createdRecord = responseItem,
                        postedData = body
                    });
                }

                return BadRequest(new { error = "Mock kaydı oluşturulamadı." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Mock kaydı işlenirken hata: {ex.Message}" });
            }
        }
    }
}
