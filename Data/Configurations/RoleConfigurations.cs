using DnTech_Ecommerce.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DnTech_Ecommerce.Data.Configurations
{
    public class RoleConfigurations : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            // Properties
            builder.Property(r => r.Description)
                .HasMaxLength(500);

            builder.Property(r => r.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETDATE()");

            // Indexes
            builder.HasIndex(r => r.Name)
                .IsUnique();
        }
    }
}
