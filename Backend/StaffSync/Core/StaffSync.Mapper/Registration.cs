using Microsoft.Extensions.DependencyInjection;
using StaffSync.Application.Interfaces.AutoMapper;

namespace StaffSync.Mapper
{
    public static class Registration
    {
        public static void AddCustomMapper(this IServiceCollection services)
        {
            services.AddSingleton<IMapper, AutoMapper.Mapper>();
        }
    }
}
