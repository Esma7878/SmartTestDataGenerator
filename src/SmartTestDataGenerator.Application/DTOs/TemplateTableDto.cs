using System.Collections.Generic;

namespace SmartTestDataGenerator.Application.DTOs
{
    public class TemplateTableDto
    {
        public int Id { get; set; }
        public int TemplateId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int RecordCount { get; set; } = 100;
        public int Order { get; set; }

        public ICollection<TemplateColumnDto> Columns { get; set; } = new List<TemplateColumnDto>();
    }
}
