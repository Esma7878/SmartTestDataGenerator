using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartTestDataGenerator.Application.DTOs;
using SmartTestDataGenerator.Application.Interfaces;
using SmartTestDataGenerator.Core.Enums;

namespace SmartTestDataGenerator.Web.Controllers
{
    public class TemplatesController : Controller
    {
        private readonly ITemplateService _templateService;

        public TemplatesController(ITemplateService templateService)
        {
            _templateService = templateService;
        }

        // GET: Templates
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Şablonlar";
            ViewData["PageHeader"] = "Veri Şablonları";

            var templates = await _templateService.GetAllWithTablesAsync();
            return View(templates);
        }

        // GET: Templates/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var template = await _templateService.GetTemplateWithDetailsAsync(id);
            if (template == null)
            {
                return NotFound();
            }

            ViewData["Title"] = $"{template.Name} Detayları";
            ViewData["PageHeader"] = template.Name;

            // Load data types to pass to custom generator modals
            ViewBag.DataTypes = Enum.GetValues(typeof(ColumnDataType))
                .Cast<ColumnDataType>()
                .Select(e => new { Id = (int)e, Name = e.ToString() })
                .ToList();

            return View(template);
        }

        // GET: Templates/Create
        public IActionResult Create()
        {
            ViewData["Title"] = "Yeni Şablon";
            ViewData["PageHeader"] = "Özel Şablon Oluştur";

            // Load data types list for UI dropdowns
            ViewBag.DataTypes = Enum.GetValues(typeof(ColumnDataType))
                .Cast<ColumnDataType>()
                .Select(e => new { Id = e, Name = e.ToString() })
                .ToList();

            return View();
        }

        // POST: Templates/Create
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TemplateDto templateDto)
        {
            if (templateDto == null || string.IsNullOrWhiteSpace(templateDto.Name))
            {
                return Json(new { success = false, message = "Geçersiz şablon verisi. Şablon adı zorunludur." });
            }

            try
            {
                // Normalize execution orders of tables and columns
                int tableOrder = 1;
                foreach (var table in templateDto.Tables)
                {
                    table.Order = tableOrder++;
                    int colOrder = 1;
                    foreach (var col in table.Columns)
                    {
                        col.Order = colOrder++;
                    }
                }

                var created = await _templateService.CreateAsync(templateDto);
                return Json(new { success = true, redirectUrl = Url.Action("Details", new { id = created.Id }) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Şablon kaydedilirken hata oluştu: {ex.Message}" });
            }
        }

        // GET: Templates/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var template = await _templateService.GetTemplateWithDetailsAsync(id);
            if (template == null || template.IsSystem)
            {
                return RedirectToAction(nameof(Index)); // Cannot edit system templates
            }

            ViewData["Title"] = $"{template.Name} Düzenle";
            ViewData["PageHeader"] = $"{template.Name} Şablonunu Düzenle";

            ViewBag.DataTypes = Enum.GetValues(typeof(ColumnDataType))
                .Cast<ColumnDataType>()
                .Select(e => new { Id = e, Name = e.ToString() })
                .ToList();

            return View(template);
        }

        // POST: Templates/Edit
        [HttpPost]
        public async Task<IActionResult> Edit([FromBody] TemplateDto templateDto)
        {
            if (templateDto == null || templateDto.Id <= 0 || string.IsNullOrWhiteSpace(templateDto.Name))
            {
                return Json(new { success = false, message = "Geçersiz şablon verisi." });
            }

            try
            {
                var existing = await _templateService.GetByIdAsync(templateDto.Id);
                if (existing == null || existing.IsSystem)
                {
                    return Json(new { success = false, message = "Sistem şablonları düzenlenemez." });
                }

                // Normalize orders
                int tableOrder = 1;
                foreach (var table in templateDto.Tables)
                {
                    table.Order = tableOrder++;
                    int colOrder = 1;
                    foreach (var col in table.Columns)
                    {
                        col.Order = colOrder++;
                    }
                }

                await _templateService.UpdateAsync(templateDto);
                return Json(new { success = true, redirectUrl = Url.Action("Details", new { id = templateDto.Id }) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Şablon güncellenirken hata oluştu: {ex.Message}" });
            }
        }

        // POST: Templates/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var template = await _templateService.GetByIdAsync(id);
            if (template == null)
            {
                return NotFound();
            }

            if (template.IsSystem)
            {
                return Json(new { success = false, message = "Sistem şablonları silinemez!" });
            }

            await _templateService.DeleteAsync(id);
            return Json(new { success = true, redirectUrl = Url.Action("Index") });
        }

        // POST: Templates/Duplicate/5
        [HttpPost]
        public async Task<IActionResult> Duplicate(int id)
        {
            var copy = await _templateService.DuplicateAsync(id);
            if (copy == null)
            {
                return Json(new { success = false, message = "Şablon bulunamadı." });
            }

            return Json(new { success = true, redirectUrl = Url.Action("Details", new { id = copy.Id }) });
        }

        // POST: Templates/TogglePin/5
        [HttpPost]
        public async Task<IActionResult> TogglePin(int id)
        {
            await _templateService.TogglePinAsync(id);
            return Json(new { success = true });
        }

        // POST: Templates/ToggleFavorite/5
        [HttpPost]
        public async Task<IActionResult> ToggleFavorite(int id)
        {
            await _templateService.ToggleFavoriteAsync(id);
            return Json(new { success = true });
        }

        // GET: Templates/ExportSchema/5
        [HttpGet]
        public async Task<IActionResult> ExportSchema(int id)
        {
            var template = await _templateService.GetTemplateWithDetailsAsync(id);
            if (template == null)
            {
                return NotFound();
            }

            // Strip database IDs for a clean portable JSON import structure
            var exportDto = new TemplateDto
            {
                Name = template.Name,
                Description = template.Description,
                Category = template.Category,
                Tables = template.Tables.Select(t => new TemplateTableDto
                {
                    Name = t.Name,
                    RecordCount = t.RecordCount,
                    Order = t.Order,
                    Columns = t.Columns.Select(c => new TemplateColumnDto
                    {
                        Name = c.Name,
                        DataType = c.DataType,
                        IsNullable = c.IsNullable,
                        NullPercentage = c.NullPercentage,
                        DuplicatePercentage = c.DuplicatePercentage,
                        MinRange = c.MinRange,
                        MaxRange = c.MaxRange,
                        CustomRule = c.CustomRule,
                        Order = c.Order
                    }).ToList()
                }).ToList()
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(exportDto, options);
            var fileName = $"{template.Name.Replace(" ", "_").ToLower()}_şablon.json";

            return File(jsonBytes, "application/json", fileName);
        }

        // POST: Templates/ImportSchema
        [HttpPost]
        public async Task<IActionResult> ImportSchema(IFormFile schemaFile)
        {
            if (schemaFile == null || schemaFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Lütfen geçerli bir JSON şablon dosyası seçin.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                using var reader = new StreamReader(schemaFile.OpenReadStream());
                var jsonString = await reader.ReadToEndAsync();
                
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var templateDto = JsonSerializer.Deserialize<TemplateDto>(jsonString, options);

                if (templateDto == null || string.IsNullOrWhiteSpace(templateDto.Name))
                {
                    TempData["ErrorMessage"] = "Şablon dosyası okunamadı veya biçimi geçersiz.";
                    return RedirectToAction(nameof(Index));
                }

                // Add imported suffix to distinguish
                templateDto.Name = $"{templateDto.Name} (Aktarılan)";
                
                var created = await _templateService.CreateAsync(templateDto);
                TempData["SuccessMessage"] = $"'{created.Name}' şablonu başarıyla içe aktarıldı.";
                return RedirectToAction("Details", new { id = created.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"İçe aktarma sırasında hata oluştu: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
