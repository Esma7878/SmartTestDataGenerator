using AutoMapper;
using SmartTestDataGenerator.Core.Entities;
using SmartTestDataGenerator.Application.DTOs;

namespace SmartTestDataGenerator.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Template, TemplateDto>().ReverseMap();
            CreateMap<TemplateTable, TemplateTableDto>().ReverseMap();
            CreateMap<TemplateColumn, TemplateColumnDto>().ReverseMap();
        }
    }
}
