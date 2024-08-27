using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StaffSync.Application.Interfaces.Tokens;
using StaffSync.Infrastructure.Tokens;

namespace StaffSync.Infrastructure
{
    public static class Registration
    {
        public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<TokenSettings>(configuration.GetSection("JWT"));
            services.AddTransient<ITokenService, TokenService>();
        }
    }
}
