using SmartTestDataGenerator.Core.Enums;

namespace SmartTestDataGenerator.Core.Entities
{
    public class TemplateColumn
    {
        public int Id { get; set; }
        public int TableId { get; set; }
        public TemplateTable Table { get; set; } = null!;

        public string Name { get; set; } = string.Empty;
        public ColumnDataType DataType { get; set; }
        public bool IsNullable { get; set; }
        public int NullPercentage { get; set; } // 0 to 100
        public int DuplicatePercentage { get; set; } // 0 to 100
        public string? MinRange { get; set; } // e.g., "18" for age, "2020-01-01" for date
        public string? MaxRange { get; set; } // e.g., "65" for age, "2025-12-31" for date
        public string? CustomRule { get; set; } // JSON or text rule for advanced customization
        public int Order { get; set; }

        // Relational Fields (Used if DataType is ForeignKey)
        public int? ParentTableId { get; set; }
        public int? ParentColumnId { get; set; }
    }
}
