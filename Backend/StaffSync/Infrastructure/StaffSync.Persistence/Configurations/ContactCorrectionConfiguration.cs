using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StaffSync.Domain.Entities;

namespace StaffSync.Persistence.Configurations
{
    public class ContactCorrectionConfiguration : IEntityTypeConfiguration<ContactCorrection>
    {
        public void Configure(EntityTypeBuilder<ContactCorrection> builder)
        {
            builder.Property(x => x.Note).HasMaxLength(500);
            builder.HasOne(x => x.Contact).WithMany().HasForeignKey(x => x.ContactId).OnDelete(DeleteBehavior.Restrict);
            builder.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.ContactCorrectionId).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(x => new { x.ReportedByUserId, x.CreatedDate });
            builder.HasIndex(x => x.Status);
        }
    }

    public class ContactCorrectionItemConfiguration : IEntityTypeConfiguration<ContactCorrectionItem>
    {
        public void Configure(EntityTypeBuilder<ContactCorrectionItem> builder)
        {
            builder.Property(x => x.FieldName).HasMaxLength(50);
            builder.Property(x => x.OldValue).HasMaxLength(200);
            builder.Property(x => x.NewValue).HasMaxLength(200);
        }
    }
}
