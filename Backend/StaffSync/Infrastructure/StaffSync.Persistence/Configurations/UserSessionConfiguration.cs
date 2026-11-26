using StaffSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StaffSync.Persistence.Configurations
{
    public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
    {
        public void Configure(EntityTypeBuilder<UserSession> builder)
        {
            builder.Property(x => x.DeviceId).HasMaxLength(64).IsRequired();
            builder.Property(x => x.RefreshTokenHash).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => new { x.UserId, x.DeviceId }).IsUnique();
            builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
