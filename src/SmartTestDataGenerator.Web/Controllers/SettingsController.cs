using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTestDataGenerator.Core.Entities;
using SmartTestDataGenerator.Core.Interfaces;
using SmartTestDataGenerator.Infrastructure.Data;

namespace SmartTestDataGenerator.Web.Controllers
{
    public class SettingsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IRepository<RecentActivity> _activityRepository;

        public SettingsController(AppDbContext context, IRepository<RecentActivity> activityRepository)
        {
            _context = context;
            _activityRepository = activityRepository;
        }

        // GET: Settings
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Ayarlar";
            ViewData["PageHeader"] = "Sistem Ayarları";

            var settingsList = await _context.Settings.ToListAsync();
            
            // Map settings list to ViewBag for easier view consumption
            ViewBag.Theme = settingsList.FirstOrDefault(s => s.Key == "Theme")?.Value ?? "dark";
            ViewBag.Language = settingsList.FirstOrDefault(s => s.Key == "Language")?.Value ?? "tr";
            ViewBag.DefaultExport = settingsList.FirstOrDefault(s => s.Key == "DefaultExport")?.Value ?? "JSON";
            ViewBag.DefaultSeed = settingsList.FirstOrDefault(s => s.Key == "DefaultSeed")?.Value ?? "42";

            return View();
        }

        // POST: Settings/Save
        [HttpPost]
        public async Task<IActionResult> Save(string theme, string language, string defaultExport, string defaultSeed)
        {
            try
            {
                var settingsList = await _context.Settings.ToListAsync();

                void UpdateOrAdd(string key, string value)
                {
                    var setting = settingsList.FirstOrDefault(s => s.Key == key);
                    if (setting != null)
                    {
                        setting.Value = value;
                    }
                    else
                    {
                        _context.Settings.Add(new Setting { Key = key, Value = value });
                    }
                }

                UpdateOrAdd("Theme", theme);
                UpdateOrAdd("Language", language);
                UpdateOrAdd("DefaultExport", defaultExport);
                UpdateOrAdd("DefaultSeed", defaultSeed);

                await _context.SaveChangesAsync();

                // Add activity log
                var activity = new RecentActivity
                {
                    ActivityType = "SettingsUpdated",
                    Details = "Sistem tercihleri başarıyla güncellendi.",
                    Timestamp = DateTime.UtcNow
                };
                await _activityRepository.AddAsync(activity);
                await _activityRepository.SaveChangesAsync();

                TempData["SuccessMessage"] = "Ayarlar başarıyla kaydedildi.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Ayarlar kaydedilirken hata oluştu: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
