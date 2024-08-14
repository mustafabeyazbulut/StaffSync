using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace StaffSync.Application.Services
{
    public static class ServiceRegistiration
    {
        public static void AddApplicationService(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(ServiceRegistiration).Assembly));
        }
    }
}
