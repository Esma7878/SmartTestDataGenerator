using System.Collections.Generic;
using System.Threading.Tasks;
using SmartTestDataGenerator.Application.DTOs;

namespace SmartTestDataGenerator.Application.Interfaces
{
    public interface ITemplateService
    {
        Task<TemplateDto?> GetByIdAsync(int id);
        Task<TemplateDto?> GetTemplateWithDetailsAsync(int id);
        Task<IEnumerable<TemplateDto>> GetAllAsync();
        Task<IEnumerable<TemplateDto>> GetAllWithTablesAsync();
        Task<IEnumerable<TemplateDto>> GetPinnedAndFavoritesAsync();
        Task<TemplateDto> CreateAsync(TemplateDto templateDto);
        Task UpdateAsync(TemplateDto templateDto);
        Task DeleteAsync(int id);
        Task<TemplateDto?> DuplicateAsync(int id);
        Task TogglePinAsync(int id);
        Task ToggleFavoriteAsync(int id);
    }
}
