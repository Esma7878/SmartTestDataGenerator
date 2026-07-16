using System.Collections.Generic;

namespace SmartTestDataGenerator.Core.Entities
{
    public class TemplateTable
    {
        public int Id { get; set; }
        public int TemplateId { get; set; }
        public Template Template { get; set; } = null!;
        
        public string Name { get; set; } = string.Empty;
        public int RecordCount { get; set; } = 100; // Default count
        public int Order { get; set; } // Execution order for topologically sorting dependencies

        public ICollection<TemplateColumn> Columns { get; set; } = new List<TemplateColumn>();
    }
}
