using System;
using System.Collections.Generic;

namespace SmartTestDataGenerator.Core.Entities
{
    public class Template
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsSystem { get; set; }
        public bool IsFavorite { get; set; }
        public bool IsPinned { get; set; }
        public string Category { get; set; } = string.Empty; // e.g. Finance, Healthcare, custom
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<TemplateTable> Tables { get; set; } = new List<TemplateTable>();
    }
}
