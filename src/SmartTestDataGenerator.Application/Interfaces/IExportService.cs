using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartTestDataGenerator.Application.Interfaces
{
    public interface IExportService
    {
        Task<byte[]> ExportToCsvAsync(Dictionary<string, List<Dictionary<string, object>>> data);
        Task<byte[]> ExportToExcelAsync(Dictionary<string, List<Dictionary<string, object>>> data);
        Task<byte[]> ExportToJsonAsync(Dictionary<string, List<Dictionary<string, object>>> data);
        Task<byte[]> ExportToXmlAsync(Dictionary<string, List<Dictionary<string, object>>> data);
        Task<byte[]> ExportToSqlAsync(Dictionary<string, List<Dictionary<string, object>>> data, string dialect);
        Task<byte[]> ExportToPdfAsync(string templateName, Dictionary<string, List<Dictionary<string, object>>> data);
    }
}
