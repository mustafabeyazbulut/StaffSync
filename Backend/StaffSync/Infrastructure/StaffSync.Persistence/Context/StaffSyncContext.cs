using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StaffSync.Domain.Entities;
using System.Reflection;

namespace StaffSync.Persistence.Context
{
    public class StaffSyncContext:DbContext
    {
        public StaffSyncContext() { }
        public StaffSyncContext(DbContextOptions options):base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
        public DbSet<Contact> Contacts { get; set; }
    }
}
