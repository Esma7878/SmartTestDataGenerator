using System.Collections.Generic;
using SmartTestDataGenerator.Application.DTOs;
using SmartTestDataGenerator.Core.Entities;

namespace SmartTestDataGenerator.Web.Models
{
    public class DashboardViewModel
    {
        public int TotalTemplates { get; set; }
        public long TotalRecords { get; set; }
        public int TotalExports { get; set; }

        public List<GenerationHistory> RecentHistory { get; set; } = new List<GenerationHistory>();
        public List<RecentActivity> RecentActivities { get; set; } = new List<RecentActivity>();
        public List<TemplateDto> PinnedTemplates { get; set; } = new List<TemplateDto>();

        // Format distribution chart
        public List<string> FormatChartLabels { get; set; } = new List<string>();
        public List<int> FormatChartValues { get; set; } = new List<int>();

        // Records timeline chart
        public List<string> TimelineChartLabels { get; set; } = new List<string>();
        public List<int> TimelineChartValues { get; set; } = new List<int>();
    }
}
