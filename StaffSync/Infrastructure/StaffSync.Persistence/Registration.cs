using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StaffSync.Application.Interfaces.Repositories;
using StaffSync.Application.Interfaces.UnitOfWorks;
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

            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped(typeof(IReadRepository<>), typeof(ReadRepository<>));
            services.AddScoped(typeof(IWriteRepository<>), typeof(WriteRepository<>));
            
            services.AddScoped<IUnitOfWork, UnitOfWork>();
        }
    }
}
