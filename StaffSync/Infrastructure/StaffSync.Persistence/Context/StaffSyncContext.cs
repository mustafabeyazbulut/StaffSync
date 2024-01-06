using Microsoft.EntityFrameworkCore;
using StaffSync.Domain.Entities;

namespace StaffSync.Persistence.Context
{
    public class StaffSyncContext:DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=localhost;Database=StaffSyncDb;User Id=sa;Password=12sa;MultipleActiveResultSets=true;TrustServerCertificate=True");
        }
        public DbSet<Contact> Contacts { get; set; }
    }
}
