using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTestDataGenerator.Core.Entities;
using SmartTestDataGenerator.Core.Interfaces;

namespace SmartTestDataGenerator.Web.Controllers
{
    public class HistoryController : Controller
    {
        private readonly IRepository<GenerationHistory> _historyRepository;
        private readonly IRepository<RecentActivity> _activityRepository;
        private readonly IWebHostEnvironment _env;

        public HistoryController(
            IRepository<GenerationHistory> historyRepository,
            IRepository<RecentActivity> activityRepository,
            IWebHostEnvironment env)
        {
            _historyRepository = historyRepository;
            _activityRepository = activityRepository;
            _env = env;
        }

        // GET: History
        public async Task<IActionResult> Index(string searchTemplate, string searchFormat)
        {
            ViewData["Title"] = "Üretim Geçmişi";
            ViewData["PageHeader"] = "Veri Üretim Geçmişi";

            var query = _historyRepository.GetAllAsync();
            var list = await query;
            var result = list.AsQueryable();

            // Apply search filters
            if (!string.IsNullOrWhiteSpace(searchTemplate))
            {
                result = result.Where(h => h.TemplateName.Contains(searchTemplate, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrWhiteSpace(searchFormat))
            {
                result = result.Where(h => h.ExportFormat.Equals(searchFormat, StringComparison.OrdinalIgnoreCase));
            }

            return View(result.OrderByDescending(h => h.CreatedAt).ToList());
        }

        // POST: History/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var history = await _historyRepository.GetByIdAsync(id);
            if (history == null)
            {
                return NotFound();
            }

            try
            {
                // Delete physical file if exists
                if (!string.IsNullOrWhiteSpace(history.FilePath))
                {
                    string physicalPath = Path.Combine(_env.WebRootPath, history.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(physicalPath))
                    {
                        System.IO.File.Delete(physicalPath);
                    }
                }

                _historyRepository.Delete(history);
                await _historyRepository.SaveChangesAsync();

                // Add activity log
                var activity = new RecentActivity
                {
                    ActivityType = "HistoryDeleted",
                    Details = $"'{history.TemplateName}' şablonuna ait geçmiş üretim kaydı ve ilişkili dosyası silindi.",
                    Timestamp = DateTime.UtcNow
                };
                await _activityRepository.AddAsync(activity);
                await _activityRepository.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Kayıt silinirken hata oluştu: {ex.Message}" });
            }
        }
    }
}
