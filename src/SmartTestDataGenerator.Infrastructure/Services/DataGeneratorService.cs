using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Bogus;
using SmartTestDataGenerator.Application.DTOs;
using SmartTestDataGenerator.Application.Interfaces;
using SmartTestDataGenerator.Core.Enums;

namespace SmartTestDataGenerator.Infrastructure.Services
{
    public class DataGeneratorService : IDataGeneratorService
    {
        public Task<Dictionary<string, List<Dictionary<string, object>>>> GenerateDataAsync(
            TemplateDto template,
            int? seed,
            string language,
            int recordMultiplier)
        {
            var result = new Dictionary<string, List<Dictionary<string, object>>>();
            
            // 1. Initialize Bogus with locale and local seed
            int seedValue = seed ?? new Random().Next();
            var faker = new Faker(language) { Random = new Randomizer(seedValue) };
            
            // 2. Sort tables topologically to ensure parent tables are generated before child tables
            var sortedTables = SortTablesTopologically(template.Tables);
            
            // Cache to keep track of generated primary key IDs for each table (to satisfy ForeignKeys)
            // Key: TableName, Value: List of Primary Key IDs (usually ints)
            var generatedIdsCache = new Dictionary<string, List<int>>();
            
            // Cache to hold generated column values to satisfy DuplicatePercentage requirements
            // Key: ColumnKey (TableName_ColumnName), Value: List of values already generated
            var columnValuesCache = new Dictionary<string, List<object>>();

            foreach (var table in sortedTables)
            {
                var rows = new List<Dictionary<string, object>>();
                var parentIdsList = new List<int>();
                int recordsToGenerate = Math.Max(1, table.RecordCount * recordMultiplier);
                
                for (int i = 1; i <= recordsToGenerate; i++)
                {
                    var row = new Dictionary<string, object>();
                    
                    // We assume an implicit primary key named "Id" for each table
                    row["Id"] = i;
                    parentIdsList.Add(i);
                    
                    foreach (var col in table.Columns.OrderBy(c => c.Order))
                    {
                        // Check NullPercentage constraint
                        if (col.IsNullable && faker.Random.Number(1, 100) <= col.NullPercentage)
                        {
                            row[col.Name] = null!;
                            continue;
                        }
                        
                        string cacheKey = $"{table.Name}_{col.Name}";
                        if (!columnValuesCache.ContainsKey(cacheKey))
                        {
                            columnValuesCache[cacheKey] = new List<object>();
                        }
                        
                        // Check DuplicatePercentage constraint
                        if (col.DuplicatePercentage > 0 && 
                            columnValuesCache[cacheKey].Count > 0 && 
                            faker.Random.Number(1, 100) <= col.DuplicatePercentage)
                        {
                            row[col.Name] = faker.PickRandom(columnValuesCache[cacheKey]);
                            continue;
                        }
                        
                        // Generate fresh value based on ColumnDataType
                        object val = GenerateValue(faker, col, generatedIdsCache, language);
                        row[col.Name] = val;
                        
                        // Add to duplicate cache if it's not null
                        if (val != null)
                        {
                            columnValuesCache[cacheKey].Add(val);
                        }
                    }
                    
                    rows.Add(row);
                }
                
                result[table.Name] = rows;
                generatedIdsCache[table.Name] = parentIdsList;
            }
            
            return Task.FromResult(result);
        }

        private object GenerateValue(
            Faker faker, 
            TemplateColumnDto col, 
            Dictionary<string, List<int>> generatedIdsCache,
            string language)
        {
            switch (col.DataType)
            {
                case ColumnDataType.Name:
                    return faker.Name.FirstName();
                case ColumnDataType.Surname:
                    return faker.Name.LastName();
                case ColumnDataType.FullName:
                    return faker.Name.FullName();
                case ColumnDataType.Gender:
                    return language == "tr" ? faker.PickRandom("Erkek", "Kadın") : faker.PickRandom("Male", "Female");
                case ColumnDataType.BirthDate:
                    return GenerateRandomDate(faker, col.MinRange, col.MaxRange, 1960, 2005);
                case ColumnDataType.Age:
                    return GenerateRandomNumber(faker, col.MinRange, col.MaxRange, 18, 80);
                case ColumnDataType.Phone:
                    return language == "tr" ? faker.Phone.PhoneNumber("05## ### ## ##") : faker.Phone.PhoneNumber();
                case ColumnDataType.Email:
                    return faker.Internet.Email();
                case ColumnDataType.Password:
                    return faker.Internet.Password(8, false);
                case ColumnDataType.Username:
                    return faker.Internet.UserName();
                case ColumnDataType.Address:
                    return faker.Address.FullAddress();
                case ColumnDataType.City:
                    return faker.Address.City();
                case ColumnDataType.Country:
                    return faker.Address.Country();
                case ColumnDataType.ZipCode:
                    return faker.Address.ZipCode();
                case ColumnDataType.Company:
                    return faker.Company.CompanyName();
                case ColumnDataType.Department:
                    return faker.Commerce.Department();
                case ColumnDataType.JobTitle:
                    return faker.Name.JobTitle();
                case ColumnDataType.Salary:
                    return GenerateRandomNumber(faker, col.MinRange, col.MaxRange, 30000, 150000);
                case ColumnDataType.Currency:
                    return faker.Finance.Currency().Code;
                case ColumnDataType.Price:
                    return GenerateRandomDecimal(faker, col.MinRange, col.MaxRange, 10, 5000);
                case ColumnDataType.ProductName:
                    return faker.Commerce.ProductName();
                case ColumnDataType.Category:
                    return faker.Commerce.Categories(1)[0];
                case ColumnDataType.ISBN:
                    return faker.Phone.PhoneNumber("978-###-###-###-#");
                case ColumnDataType.UUID:
                case ColumnDataType.GUID:
                    return Guid.NewGuid().ToString();
                case ColumnDataType.CreditCard:
                    return faker.Finance.CreditCardNumber();
                case ColumnDataType.IBAN:
                    return faker.Finance.Iban();
                case ColumnDataType.SWIFT:
                    return faker.Finance.Bic();
                case ColumnDataType.URL:
                    return faker.Internet.Url();
                case ColumnDataType.Latitude:
                    return faker.Address.Latitude().ToString(CultureInfo.InvariantCulture);
                case ColumnDataType.Longitude:
                    return faker.Address.Longitude().ToString(CultureInfo.InvariantCulture);
                case ColumnDataType.Boolean:
                    return faker.Random.Bool();
                case ColumnDataType.DateTime:
                    return GenerateRandomDate(faker, col.MinRange, col.MaxRange, 2025, 2026);
                case ColumnDataType.RandomNumber:
                    return GenerateRandomNumber(faker, col.MinRange, col.MaxRange, 1, 1000);
                case ColumnDataType.RandomText:
                    if (!string.IsNullOrWhiteSpace(col.CustomRule))
                    {
                        // Custom choices split by comma
                        var choices = col.CustomRule.Split(',').Select(x => x.Trim()).ToList();
                        if (choices.Any()) return faker.PickRandom(choices);
                    }
                    return string.Join(" ", faker.Lorem.Words(3));
                case ColumnDataType.LoremIpsum:
                    return faker.Lorem.Paragraph();
                case ColumnDataType.ImageUrl:
                    return faker.Image.PicsumUrl();
                case ColumnDataType.Barcode:
                    return faker.Random.Replace("#############");
                case ColumnDataType.QRText:
                    return faker.Random.AlphaNumeric(20);
                case ColumnDataType.LicensePlate:
                    return language == "tr" 
                        ? faker.Random.Replace("## ") + faker.Random.Replace("???").ToUpper() + faker.Random.Replace(" ##") 
                        : faker.Random.Replace("##-???-##");
                case ColumnDataType.Color:
                    return faker.Commerce.Color();
                case ColumnDataType.FileName:
                    return faker.System.CommonFileName();
                case ColumnDataType.IPAddress:
                    return faker.Internet.Ip();
                case ColumnDataType.MACAddress:
                    return faker.Internet.Mac();
                case ColumnDataType.Browser:
                    return faker.Internet.UserAgent();
                case ColumnDataType.OperatingSystem:
                    return faker.PickRandom("Windows 11", "Windows 10", "macOS Sequoia", "Ubuntu 24.04", "iOS 17", "Android 14");
                
                case ColumnDataType.ForeignKey:
                    if (col.ParentTableId.HasValue && !string.IsNullOrEmpty(col.ParentTableName))
                    {
                        if (generatedIdsCache.TryGetValue(col.ParentTableName, out var parentIds) && parentIds.Any())
                        {
                            // Return a random ID generated in the parent table
                            return faker.PickRandom(parentIds);
                        }
                    }
                    // Fallback to random integer if parent is not generated or cache missed
                    return faker.Random.Number(1, 100);

                default:
                    return faker.Lorem.Word();
            }
        }

        private int GenerateRandomNumber(Faker faker, string? minStr, string? maxStr, int defaultMin, int defaultMax)
        {
            int min = defaultMin;
            int max = defaultMax;

            if (int.TryParse(minStr, out int parsedMin)) min = parsedMin;
            if (int.TryParse(maxStr, out int parsedMax)) max = parsedMax;

            if (min > max) { int temp = min; min = max; max = temp; }
            return faker.Random.Number(min, max);
        }

        private decimal GenerateRandomDecimal(Faker faker, string? minStr, string? maxStr, double defaultMin, double defaultMax)
        {
            double min = defaultMin;
            double max = defaultMax;

            if (double.TryParse(minStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedMin)) min = parsedMin;
            if (double.TryParse(maxStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedMax)) max = parsedMax;

            if (min > max) { double temp = min; min = max; max = temp; }
            return Math.Round((decimal)faker.Random.Double(min, max), 2);
        }

        private DateTime GenerateRandomDate(Faker faker, string? minStr, string? maxStr, int defaultMinYear, int defaultMaxYear)
        {
            DateTime minDate = new DateTime(defaultMinYear, 1, 1);
            DateTime maxDate = new DateTime(defaultMaxYear, 12, 31);

            if (DateTime.TryParse(minStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedMin)) minDate = parsedMin;
            if (DateTime.TryParse(maxStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedMax)) maxDate = parsedMax;

            if (minDate > maxDate) { DateTime temp = minDate; minDate = maxDate; maxDate = temp; }
            return faker.Date.Between(minDate, maxDate);
        }

        // Topological Sorting helper using DFS
        private List<TemplateTableDto> SortTablesTopologically(ICollection<TemplateTableDto> tables)
        {
            var sorted = new List<TemplateTableDto>();
            var visited = new Dictionary<int, bool>(); // false = visiting, true = visited
            
            void Visit(TemplateTableDto table)
            {
                if (visited.ContainsKey(table.Id))
                {
                    if (!visited[table.Id])
                    {
                        // Cycle detected. We break recursion silently to prevent stack overflow
                        return;
                    }
                    return;
                }
                
                visited[table.Id] = false; // Mark as visiting
                
                // Collect dependencies
                var dependencies = table.Columns
                    .Where(c => c.DataType == ColumnDataType.ForeignKey && c.ParentTableId.HasValue)
                    .Select(c => c.ParentTableId!.Value)
                    .Distinct()
                    .ToList();
                
                foreach (var parentId in dependencies)
                {
                    var parentTable = tables.FirstOrDefault(t => t.Id == parentId);
                    if (parentTable != null)
                    {
                        Visit(parentTable);
                    }
                }
                
                visited[table.Id] = true; // Visited
                sorted.Add(table);
            }
            
            foreach (var table in tables.OrderBy(t => t.Order))
            {
                if (!visited.ContainsKey(table.Id))
                {
                    Visit(table);
                }
            }
            
            return sorted;
        }
    }
}
