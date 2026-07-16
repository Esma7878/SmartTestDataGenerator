using System;

namespace SmartTestDataGenerator.Core.Entities
{
    public class GenerationHistory
    {
        public int Id { get; set; }
        public int? TemplateId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public int TotalRecords { get; set; }
        public string ExportFormat { get; set; } = string.Empty; // CSV, Excel, JSON, XML, SQL, PDF
        public long GenerationSpeedMs { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string FilePath { get; set; } = string.Empty; // Path to download generated dataset
        public int Seed { get; set; }
    }
}
