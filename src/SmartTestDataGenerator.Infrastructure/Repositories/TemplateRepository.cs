using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartTestDataGenerator.Core.Entities;
using SmartTestDataGenerator.Core.Interfaces;
using SmartTestDataGenerator.Infrastructure.Data;

namespace SmartTestDataGenerator.Infrastructure.Repositories
{
    public class TemplateRepository : Repository<Template>, ITemplateRepository
    {
        public TemplateRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Template?> GetTemplateWithDetailsAsync(int id)
        {
            var template = await _context.Templates
                .Include(t => t.Tables)
                    .ThenInclude(tb => tb.Columns)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (template != null)
            {
                // Sort tables and columns in-memory to ensure proper ordering
                template.Tables = template.Tables.OrderBy(tb => tb.Order).ToList();
                foreach (var table in template.Tables)
                {
                    table.Columns = table.Columns.OrderBy(c => c.Order).ToList();
                }
            }

            return template;
        }

        public async Task<IEnumerable<Template>> GetPinnedAndFavoritesAsync()
        {
            return await _context.Templates
                .Include(t => t.Tables)
                .Where(t => t.IsPinned || t.IsFavorite)
                .OrderByDescending(t => t.IsPinned)
                .ThenByDescending(t => t.IsFavorite)
                .ThenBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Template>> GetAllWithTablesAsync()
        {
            return await _context.Templates
                .Include(t => t.Tables)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }
    }
}
