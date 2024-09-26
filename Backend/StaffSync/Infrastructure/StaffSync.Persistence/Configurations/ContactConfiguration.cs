using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StaffSync.Domain.Entities;


namespace StaffSync.Persistence.Configurations
{
    public class ContactConfiguration : IEntityTypeConfiguration<Contact>
    {
        public void Configure(EntityTypeBuilder<Contact> builder)
        {
            builder.Property(x=>x.DisplayName).HasMaxLength(50);
            builder.Property(x=>x.JobTitle).HasMaxLength(50);
            builder.Property(x=>x.Department).HasMaxLength(50);
            builder.Property(x=>x.Company).HasMaxLength(50);
            //builder.Property(x=>x.Email).HasColumnType("text");
            //builder.Property(x=>x.TelephoneNumber).HasColumnType("text");
            //builder.Property(x=>x.CoverImageUrl).HasColumnType("text");
        }
    }
}
