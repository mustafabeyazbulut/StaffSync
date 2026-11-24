using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StaffSync.Domain.Entities;
using System.Reflection;

namespace StaffSync.Persistence.Context
{
    public class StaffSyncContext:IdentityDbContext<User,Role,Guid>
    {
        public StaffSyncContext() { }
        public StaffSyncContext(DbContextOptions options):base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<ContactCorrection> ContactCorrections { get; set; }
        public DbSet<ContactCorrectionItem> ContactCorrectionItems { get; set; }

        public override int SaveChanges()
        {
            StampContacts();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            StampContacts();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void StampContacts()
        {
            DateTime now = DateTime.UtcNow;
            foreach (var entry in ChangeTracker.Entries<Contact>())
                if (entry.State is EntityState.Added or EntityState.Modified)
                    entry.Entity.UpdatedDate = now;
        }
    }
}
