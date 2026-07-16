using System;

namespace SmartTestDataGenerator.Core.Entities
{
    public class RecentActivity
    {
        public int Id { get; set; }
        public string ActivityType { get; set; } = string.Empty; // e.g. "TemplateCreated", "DataGenerated"
        public string Details { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
