using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTestDataGenerator.Application.DTOs;
using SmartTestDataGenerator.Application.Interfaces;
using SmartTestDataGenerator.Infrastructure.Data;
using SmartTestDataGenerator.Web.Models;

namespace SmartTestDataGenerator.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ITemplateService _templateService;
        private readonly IMapper _mapper;

        public HomeController(AppDbContext context, ITemplateService templateService, IMapper mapper)
        {
            _context = context;
            _templateService = templateService;
            _mapper = mapper;
        }

        // GET: Home/Index
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Panel";
            ViewData["PageHeader"] = "Hızlı İstatistikler & Özet";

            // 1. Fetch counts
            int totalTemplates = await _context.Templates.CountAsync();
            long totalRecords = await _context.GenerationHistories.SumAsync(h => (long)h.TotalRecords);
            int totalExports = await _context.GenerationHistories.CountAsync();

            // 2. Lists
            var recentHistory = await _context.GenerationHistories
                .OrderByDescending(h => h.CreatedAt)
                .Take(5)
                .ToListAsync();

            var recentActivities = await _context.RecentActivities
                .OrderByDescending(a => a.Timestamp)
                .Take(8)
                .ToListAsync();

            var pinnedEntities = await _context.Templates
                .Include(t => t.Tables)
                .Where(t => t.IsPinned)
                .ToListAsync();
            var pinnedTemplates = _mapper.Map<List<TemplateDto>>(pinnedEntities);

            // 3. Format distribution chart data
            var formatGroups = await _context.GenerationHistories
                .GroupBy(h => h.ExportFormat)
                .Select(g => new { Format = g.Key, Count = g.Count() })
                .ToListAsync();

            var formatLabels = formatGroups.Select(g => g.Format).ToList();
            var formatValues = formatGroups.Select(g => g.Count).ToList();

            // 4. Timeline chart data (last 7 days of record generation)
            var cutoffDate = DateTime.UtcNow.AddDays(-7).Date;
            var timelineData = await _context.GenerationHistories
                .Where(h => h.CreatedAt >= cutoffDate)
                .ToListAsync();

            var timelineGroups = timelineData
                .GroupBy(h => h.CreatedAt.Date)
                .Select(g => new { Date = g.Key.ToString("dd.MM"), Total = g.Sum(h => h.TotalRecords) })
                .OrderBy(x => x.Date)
                .ToList();

            // Fill default 7 days if empty
            var timelineLabels = new List<string>();
            var timelineValues = new List<int>();

            if (!timelineGroups.Any())
            {
                for (int i = 6; i >= 0; i--)
                {
                    timelineLabels.Add(DateTime.UtcNow.AddDays(-i).ToString("dd.MM"));
                    timelineValues.Add(0);
                }
            }
            else
            {
                timelineLabels = timelineGroups.Select(g => g.Date).ToList();
                timelineValues = timelineGroups.Select(g => g.Total).ToList();
            }

            var viewModel = new DashboardViewModel
            {
                TotalTemplates = totalTemplates,
                TotalRecords = totalRecords,
                TotalExports = totalExports,
                RecentHistory = recentHistory,
                RecentActivities = recentActivities,
                PinnedTemplates = pinnedTemplates,
                FormatChartLabels = formatLabels,
                FormatChartValues = formatValues,
                TimelineChartLabels = timelineLabels,
                TimelineChartValues = timelineValues
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
