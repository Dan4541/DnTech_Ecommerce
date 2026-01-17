using DnTech_Ecommerce.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DnTech_Ecommerce.Data.Configurations
{
    public class OrderConfigurations : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.HasKey(o => o.Id);

            builder.Property(o => o.OrderNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(o => o.ShippingFullName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(o => o.ShippingEmail)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(o => o.ShippingPhone)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(o => o.ShippingAddress)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(o => o.ShippingCity)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(o => o.ShippingState)
                .HasMaxLength(50);

            builder.Property(o => o.ShippingPostalCode)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(o => o.ShippingCountry)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(o => o.Subtotal)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(o => o.ShippingCost)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(o => o.Tax)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(o => o.Total)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(o => o.Status)
                .IsRequired();

            builder.Property(o => o.PaymentMethod)
                .IsRequired();

            builder.Property(o => o.PaymentStatus)
                .IsRequired();

            builder.Property(o => o.Notes)
                .HasMaxLength(500);

            builder.Property(o => o.OrderDate)
                .IsRequired()
                .HasDefaultValueSql("GETDATE()");

            // Relación con User
            builder.HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación con OrderItems
            builder.HasMany(o => o.Items)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Índices para búsquedas comunes
            builder.HasIndex(o => o.OrderNumber)
                .IsUnique();

            builder.HasIndex(o => o.UserId);

            builder.HasIndex(o => o.OrderDate);

            builder.HasIndex(o => o.Status);
        }
    }
}
