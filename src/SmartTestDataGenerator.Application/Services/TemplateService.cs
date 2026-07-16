using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using SmartTestDataGenerator.Application.DTOs;
using SmartTestDataGenerator.Application.Interfaces;
using SmartTestDataGenerator.Core.Entities;
using SmartTestDataGenerator.Core.Enums;
using SmartTestDataGenerator.Core.Interfaces;

namespace SmartTestDataGenerator.Application.Services
{
    public class TemplateService : ITemplateService
    {
        private readonly ITemplateRepository _templateRepository;
        private readonly IRepository<TemplateTable> _tableRepository;
        private readonly IRepository<TemplateColumn> _columnRepository;
        private readonly IMapper _mapper;

        public TemplateService(
            ITemplateRepository templateRepository,
            IRepository<TemplateTable> tableRepository,
            IRepository<TemplateColumn> columnRepository,
            IMapper mapper)
        {
            _templateRepository = templateRepository;
            _tableRepository = tableRepository;
            _columnRepository = columnRepository;
            _mapper = mapper;
        }

        public async Task<TemplateDto?> GetByIdAsync(int id)
        {
            var template = await _templateRepository.GetByIdAsync(id);
            return _mapper.Map<TemplateDto>(template);
        }

        public async Task<TemplateDto?> GetTemplateWithDetailsAsync(int id)
        {
            var template = await _templateRepository.GetTemplateWithDetailsAsync(id);
            if (template == null) return null;

            var dto = _mapper.Map<TemplateDto>(template);

            // Populate ParentTableName and ParentColumnName for FK columns in the UI DTO
            var allTables = dto.Tables.ToList();
            var allCols = allTables.SelectMany(t => t.Columns).ToList();

            foreach (var col in allCols)
            {
                if (col.DataType == ColumnDataType.ForeignKey)
                {
                    if (col.ParentTableId.HasValue)
                    {
                        var parentTable = allTables.FirstOrDefault(t => t.Id == col.ParentTableId.Value);
                        if (parentTable != null)
                        {
                            col.ParentTableName = parentTable.Name;
                            var parentCol = parentTable.Columns.FirstOrDefault(c => c.Id == col.ParentColumnId);
                            col.ParentColumnName = parentCol?.Name ?? "Id";
                        }
                        else
                        {
                            // If the parent table is not loaded in this template, try loading it from DB
                            var pTable = await _tableRepository.GetByIdAsync(col.ParentTableId.Value);
                            if (pTable != null)
                            {
                                col.ParentTableName = pTable.Name;
                                if (col.ParentColumnId.HasValue)
                                {
                                    var pCol = await _columnRepository.GetByIdAsync(col.ParentColumnId.Value);
                                    col.ParentColumnName = pCol?.Name ?? "Id";
                                }
                                else
                                {
                                    col.ParentColumnName = "Id";
                                }
                            }
                        }
                    }
                }
            }

            return dto;
        }

        public async Task<IEnumerable<TemplateDto>> GetAllAsync()
        {
            var templates = await _templateRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<TemplateDto>>(templates);
        }

        public async Task<IEnumerable<TemplateDto>> GetAllWithTablesAsync()
        {
            var templates = await _templateRepository.GetAllWithTablesAsync();
            return _mapper.Map<IEnumerable<TemplateDto>>(templates);
        }

        public async Task<IEnumerable<TemplateDto>> GetPinnedAndFavoritesAsync()
        {
            var templates = await _templateRepository.GetPinnedAndFavoritesAsync();
            return _mapper.Map<IEnumerable<TemplateDto>>(templates);
        }

        public async Task<TemplateDto> CreateAsync(TemplateDto templateDto)
        {
            var template = _mapper.Map<Template>(templateDto);
            template.CreatedAt = DateTime.UtcNow;
            template.UpdatedAt = DateTime.UtcNow;
            template.IsSystem = false; // User-created templates are not system templates

            await _templateRepository.AddAsync(template);
            await _templateRepository.SaveChangesAsync();

            // Resolve temporary parent table name mappings
            ResolveForeignKeys(template);
            await _templateRepository.SaveChangesAsync();

            return _mapper.Map<TemplateDto>(template);
        }

        public async Task UpdateAsync(TemplateDto templateDto)
        {
            var existingTemplate = await _templateRepository.GetTemplateWithDetailsAsync(templateDto.Id);
            if (existingTemplate == null) return;

            // Update template properties
            existingTemplate.Name = templateDto.Name;
            existingTemplate.Description = templateDto.Description;
            existingTemplate.Category = templateDto.Category;
            existingTemplate.UpdatedAt = DateTime.UtcNow;

            // Delete tables not present in the updated DTO
            var updatedTableIds = templateDto.Tables.Select(t => t.Id).ToList();
            var tablesToDelete = existingTemplate.Tables.Where(t => !updatedTableIds.Contains(t.Id)).ToList();
            foreach (var tableToDelete in tablesToDelete)
            {
                _tableRepository.Delete(tableToDelete);
            }

            // Update existing tables and insert new ones
            foreach (var tableDto in templateDto.Tables)
            {
                if (tableDto.Id > 0) // Existing table
                {
                    var existingTable = existingTemplate.Tables.FirstOrDefault(t => t.Id == tableDto.Id);
                    if (existingTable != null)
                    {
                        existingTable.Name = tableDto.Name;
                        existingTable.RecordCount = tableDto.RecordCount;
                        existingTable.Order = tableDto.Order;

                        // Delete columns not present in the updated DTO
                        var updatedColIds = tableDto.Columns.Select(c => c.Id).ToList();
                        var colsToDelete = existingTable.Columns.Where(c => !updatedColIds.Contains(c.Id)).ToList();
                        foreach (var colToDelete in colsToDelete)
                        {
                            _columnRepository.Delete(colToDelete);
                        }

                        // Update existing columns and insert new ones
                        foreach (var colDto in tableDto.Columns)
                        {
                            if (colDto.Id > 0) // Existing column
                            {
                                var existingCol = existingTable.Columns.FirstOrDefault(c => c.Id == colDto.Id);
                                if (existingCol != null)
                                {
                                    existingCol.Name = colDto.Name;
                                    existingCol.DataType = colDto.DataType;
                                    existingCol.IsNullable = colDto.IsNullable;
                                    existingCol.NullPercentage = colDto.NullPercentage;
                                    existingCol.DuplicatePercentage = colDto.DuplicatePercentage;
                                    existingCol.MinRange = colDto.MinRange;
                                    existingCol.MaxRange = colDto.MaxRange;
                                    existingCol.CustomRule = colDto.CustomRule;
                                    existingCol.Order = colDto.Order;
                                    existingCol.ParentTableId = colDto.ParentTableId;
                                    existingCol.ParentColumnId = colDto.ParentColumnId;
                                }
                            }
                            else // New column
                            {
                                var newCol = _mapper.Map<TemplateColumn>(colDto);
                                newCol.TableId = existingTable.Id;
                                existingTable.Columns.Add(newCol);
                            }
                        }
                    }
                }
                else // New table
                {
                    var newTable = _mapper.Map<TemplateTable>(tableDto);
                    newTable.TemplateId = existingTemplate.Id;
                    existingTemplate.Tables.Add(newTable);
                }
            }

            _templateRepository.Update(existingTemplate);
            await _templateRepository.SaveChangesAsync();

            // Resolve temporary parent table name mappings for any new columns added
            ResolveForeignKeys(existingTemplate);
            await _templateRepository.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var template = await _templateRepository.GetByIdAsync(id);
            if (template != null)
            {
                _templateRepository.Delete(template);
                await _templateRepository.SaveChangesAsync();
            }
        }

        public async Task<TemplateDto?> DuplicateAsync(int id)
        {
            var original = await _templateRepository.GetTemplateWithDetailsAsync(id);
            if (original == null) return null;

            // 1. Create template copy
            var copy = new Template
            {
                Name = $"{original.Name} (Kopya)",
                Description = original.Description,
                Category = original.Category,
                IsSystem = false,
                IsFavorite = false,
                IsPinned = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _templateRepository.AddAsync(copy);
            await _templateRepository.SaveChangesAsync();

            // Mappings of old IDs to new entities to preserve FK references
            var tableMap = new Dictionary<int, TemplateTable>();
            var columnMap = new Dictionary<int, TemplateColumn>();

            // 2. Duplicate Tables
            foreach (var origTable in original.Tables)
            {
                var copyTable = new TemplateTable
                {
                    TemplateId = copy.Id,
                    Name = origTable.Name,
                    RecordCount = origTable.RecordCount,
                    Order = origTable.Order
                };

                await _tableRepository.AddAsync(copyTable);
                await _tableRepository.SaveChangesAsync();
                tableMap[origTable.Id] = copyTable;

                // 3. Duplicate Columns
                foreach (var origCol in origTable.Columns)
                {
                    var copyCol = new TemplateColumn
                    {
                        TableId = copyTable.Id,
                        Name = origCol.Name,
                        DataType = origCol.DataType,
                        IsNullable = origCol.IsNullable,
                        NullPercentage = origCol.NullPercentage,
                        DuplicatePercentage = origCol.DuplicatePercentage,
                        MinRange = origCol.MinRange,
                        MaxRange = origCol.MaxRange,
                        CustomRule = origCol.CustomRule,
                        Order = origCol.Order,
                        ParentTableId = origCol.ParentTableId,
                        ParentColumnId = origCol.ParentColumnId
                    };

                    await _columnRepository.AddAsync(copyCol);
                    await _columnRepository.SaveChangesAsync();
                    columnMap[origCol.Id] = copyCol;
                }
            }

            // 4. Resolve and remap FKs within the new template
            foreach (var copyCol in columnMap.Values)
            {
                if (copyCol.DataType == ColumnDataType.ForeignKey)
                {
                    if (copyCol.ParentTableId.HasValue && tableMap.ContainsKey(copyCol.ParentTableId.Value))
                    {
                        copyCol.ParentTableId = tableMap[copyCol.ParentTableId.Value].Id;
                    }
                    if (copyCol.ParentColumnId.HasValue && columnMap.ContainsKey(copyCol.ParentColumnId.Value))
                    {
                        copyCol.ParentColumnId = columnMap[copyCol.ParentColumnId.Value].Id;
                    }
                    _columnRepository.Update(copyCol);
                }
            }

            await _columnRepository.SaveChangesAsync();

            return await GetTemplateWithDetailsAsync(copy.Id);
        }

        public async Task TogglePinAsync(int id)
        {
            var template = await _templateRepository.GetByIdAsync(id);
            if (template != null)
            {
                template.IsPinned = !template.IsPinned;
                template.UpdatedAt = DateTime.UtcNow;
                _templateRepository.Update(template);
                await _templateRepository.SaveChangesAsync();
            }
        }

        public async Task ToggleFavoriteAsync(int id)
        {
            var template = await _templateRepository.GetByIdAsync(id);
            if (template != null)
            {
                template.IsFavorite = !template.IsFavorite;
                template.UpdatedAt = DateTime.UtcNow;
                _templateRepository.Update(template);
                await _templateRepository.SaveChangesAsync();
            }
        }

        private void ResolveForeignKeys(Template template)
        {
            var tables = template.Tables.ToList();
            var columns = tables.SelectMany(t => t.Columns).ToList();

            foreach (var col in columns)
            {
                if (col.DataType == ColumnDataType.ForeignKey && !string.IsNullOrEmpty(col.CustomRule))
                {
                    try
                    {
                        // CustomRule contains JSON: {"parentTableName": "..."}
                        using var doc = System.Text.Json.JsonDocument.Parse(col.CustomRule);
                        if (doc.RootElement.TryGetProperty("parentTableName", out var prop))
                        {
                            var parentTableName = prop.GetString();
                            var parentTable = tables.FirstOrDefault(t => t.Name.Equals(parentTableName, StringComparison.OrdinalIgnoreCase));
                            if (parentTable != null)
                            {
                                col.ParentTableId = parentTable.Id;
                                // Find parent's primary key (usually Id)
                                var parentIdCol = parentTable.Columns.FirstOrDefault(c => c.Name.Equals("Id", StringComparison.OrdinalIgnoreCase));
                                if (parentIdCol != null)
                                {
                                    col.ParentColumnId = parentIdCol.Id;
                                }
                            }
                            
                            // Clear the temporary JSON rule
                            col.CustomRule = null;
                        }
                    }
                    catch
                    {
                        // If not valid JSON, leave as is
                    }
                }
            }
        }
    }
}
