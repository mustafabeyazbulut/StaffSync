using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StaffSync.Application.Interfaces.Repositories;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;
using StaffSync.Persistence.Context;
using StaffSync.Persistence.Repositories;
using StaffSync.Persistence.UnitOfWorks;

namespace StaffSync.Persistence
{
    public static class Registration
    {
        public static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<StaffSyncContext>(opt => opt.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))); //appsettingden çekiyor

            services.AddScoped(typeof(IReadRepository<>), typeof(ReadRepository<>));
            services.AddScoped(typeof(IWriteRepository<>), typeof(WriteRepository<>));
            
            services.AddScoped<IUnitOfWork, UnitOfWork>(); // UnitOfWork, bir işlem sırasında birden fazla repository sınıfı üzerinde işlem yapılmasını sağlar.

            services.AddIdentityCore<User>(opt=>
            {
                opt.Password.RequireNonAlphanumeric = false;
                opt.Password.RequiredLength = 2;
                opt.Password.RequireLowercase = false;
                opt.Password.RequireUppercase = false;
                opt.Password.RequireDigit = false;
                opt.SignIn.RequireConfirmedEmail = false;
            }
                
                ).AddRoles<Role>().AddEntityFrameworkStores<StaffSyncContext>();  // AddIdentityCore<User> : User sınıfını IdentityUser sınıfından türetir.

        }
    }
}
