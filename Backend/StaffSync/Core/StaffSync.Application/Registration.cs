using Microsoft.Extensions.DependencyInjection;
using StaffSync.Application.Exceptions;
using System.Reflection;

namespace StaffSync.Application
{
    public static class Registration
    {
        public static void AddApplication(this IServiceCollection services)
        {
            var assembly= Assembly.GetExecutingAssembly();

            services.AddTransient < ExceptionMiddleware>();

            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        }
    }
}
