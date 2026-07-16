using System.Collections.Generic;
using System.Threading.Tasks;
using SmartTestDataGenerator.Core.Entities;

namespace SmartTestDataGenerator.Core.Interfaces
{
    public interface ITemplateRepository : IRepository<Template>
    {
        Task<Template?> GetTemplateWithDetailsAsync(int id);
        Task<IEnumerable<Template>> GetPinnedAndFavoritesAsync();
        Task<IEnumerable<Template>> GetAllWithTablesAsync();
    }
}
