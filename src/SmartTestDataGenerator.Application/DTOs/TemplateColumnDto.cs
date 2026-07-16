using SmartTestDataGenerator.Core.Enums;

namespace SmartTestDataGenerator.Application.DTOs
{
    public class TemplateColumnDto
    {
        public int Id { get; set; }
        public int TableId { get; set; }
        public string Name { get; set; } = string.Empty;
        public ColumnDataType DataType { get; set; }
        public bool IsNullable { get; set; }
        public int NullPercentage { get; set; }
        public int DuplicatePercentage { get; set; }
        public string? MinRange { get; set; }
        public string? MaxRange { get; set; }
        public string? CustomRule { get; set; }
        public int Order { get; set; }

        public int? ParentTableId { get; set; }
        public int? ParentColumnId { get; set; }

        // Helper properties for UI rendering
        public string? ParentTableName { get; set; }
        public string? ParentColumnName { get; set; }
    }
}
