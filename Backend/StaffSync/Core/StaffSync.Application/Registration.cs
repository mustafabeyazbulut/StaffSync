using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using StaffSync.Application.Beheviors;
using StaffSync.Application.Exceptions;
using System.Globalization;
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

            services.AddValidatorsFromAssembly(assembly);
            ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("tr");
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(FluentValidationBehevior<,>));

        }
    }
}
