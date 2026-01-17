using DnTech_Ecommerce.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DnTech_Ecommerce.Data.Configurations
{
    public class OrderItemConfigurations : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.HasKey(oi => oi.Id);

            builder.Property(oi => oi.ProductName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(oi => oi.ProductSku)
                .HasMaxLength(50);

            builder.Property(oi => oi.Price)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(oi => oi.Quantity)
                .IsRequired()
                .HasDefaultValue(1);

            // Relación con Order
            builder.HasOne(oi => oi.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relación con Product
            builder.HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Índices
            builder.HasIndex(oi => oi.OrderId);

            builder.HasIndex(oi => oi.ProductId);
        }
    }
}
