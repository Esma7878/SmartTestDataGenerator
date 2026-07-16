using System;
using System.Collections.Generic;

namespace SmartTestDataGenerator.Application.DTOs
{
    public class TemplateDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsSystem { get; set; }
        public bool IsFavorite { get; set; }
        public bool IsPinned { get; set; }
        public string Category { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<TemplateTableDto> Tables { get; set; } = new List<TemplateTableDto>();
    }
}
