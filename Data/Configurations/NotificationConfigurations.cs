using DnTech_Ecommerce.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DnTech_Ecommerce.Data.Configurations
{
    public class NotificationConfigurations : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            // Clave primaria
            builder.HasKey(n => n.Id);

            // Propiedades requeridas
            builder.Property(n => n.UserId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(n => n.Message)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(n => n.Type)
                .IsRequired();

            builder.Property(n => n.Link)
                .HasMaxLength(200);

            builder.Property(n => n.IsRead)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(n => n.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETDATE()");

            // Relación con User (Many-to-One)
            builder.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Si se elimina el usuario, se eliminan sus notificaciones

            // Relación con Order (Many-to-One) - Opcional
            builder.HasOne(n => n.Order)
                .WithMany()
                .HasForeignKey(n => n.OrderId)
                .OnDelete(DeleteBehavior.SetNull) // Si se elimina la orden, OrderId se pone en NULL
                .IsRequired(false);

            // Índices para mejorar rendimiento
            builder.HasIndex(n => n.UserId)
                .HasDatabaseName("IX_Notifications_UserId");

            builder.HasIndex(n => n.IsRead)
                .HasDatabaseName("IX_Notifications_IsRead");

            builder.HasIndex(n => n.CreatedAt)
                .HasDatabaseName("IX_Notifications_CreatedAt");

            // Índice compuesto para consultas comunes (usuario + no leídas)
            builder.HasIndex(n => new { n.UserId, n.IsRead })
                .HasDatabaseName("IX_Notifications_UserId_IsRead");
        }
    }
}