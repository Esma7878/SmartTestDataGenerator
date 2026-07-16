using System.Collections.Generic;
using System.Threading.Tasks;
using SmartTestDataGenerator.Application.DTOs;

namespace SmartTestDataGenerator.Application.Interfaces
{
    public interface IDataGeneratorService
    {
        Task<Dictionary<string, List<Dictionary<string, object>>>> GenerateDataAsync(
            TemplateDto template,
            int? seed,
            string language,
            int recordMultiplier);
    }
}
