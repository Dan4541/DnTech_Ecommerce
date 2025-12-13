using DnTech_Ecommerce.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DnTech_Ecommerce.Data.Configurations
{
    public class ProductConfigurations : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            // Clave primaria
            builder.HasKey(p => p.Id);

            // Propiedades
            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(p => p.Description)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(p => p.Price)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(p => p.OldPrice)
                .HasColumnType("decimal(18,2)");

            builder.Property(p => p.StockQuantity)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(p => p.Sku)
                .HasMaxLength(100);

            builder.Property(p => p.Slug)
                .HasMaxLength(200);

            builder.Property(p => p.MainImageUrl)
                .HasMaxLength(500);

            builder.Property(p => p.AdditionalImages)
                .HasMaxLength(2000);

            builder.Property(p => p.Rating)
                .HasColumnType("decimal(3,2)")
                .HasDefaultValue(0);

            builder.Property(p => p.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETDATE()");

            // Índices
            builder.HasIndex(p => p.Slug).IsUnique();
            builder.HasIndex(p => p.Sku).IsUnique();
            builder.HasIndex(p => p.Name);
            builder.HasIndex(p => p.CategoryId);
            builder.HasIndex(p => p.IsFeatured);
            builder.HasIndex(p => p.IsOnSale);
            builder.HasIndex(p => p.IsActive);

            // Relaciones
            builder.HasOne(p => p.Category)
                   .WithMany(c => c.Products)
                   .HasForeignKey(p => p.CategoryId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Datos iniciales (Seed Data) - USAR VALORES ESTÁTICOS
            var seedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            builder.HasData(
                new Product
                {
                    Id = 1,
                    Name = "iPhone 14 Pro",
                    Description = "256GB, Pantalla Dynamic Island, Cámara profesional",
                    Price = 999.99m,
                    OldPrice = 1199.99m,
                    StockQuantity = 50,
                    Sku = "IPH14P256",
                    Slug = "iphone-14-pro",
                    MainImageUrl = "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80",
                    IsOnSale = true,
                    IsFeatured = true,
                    IsNew = true,
                    IsActive = true,
                    CategoryId = 1,
                    CreatedAt = seedDate.AddDays(-10)
                },
                new Product
                {
                    Id = 2,
                    Name = "MacBook Pro M2",
                    Description = "16GB RAM, 512GB SSD, Chip M2",
                    Price = 1299.99m,
                    StockQuantity = 30,
                    Sku = "MBPM216512",
                    Slug = "macbook-pro-m2",
                    MainImageUrl = "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80",
                    IsFeatured = true,
                    IsNew = true,
                    IsActive = true,
                    CategoryId = 2,
                    CreatedAt = seedDate.AddDays(-5)
                },
                new Product
                {
                    Id = 3,
                    Name = "Sony WH-1000XM4",
                    Description = "Auriculares con cancelación de ruido",
                    Price = 349.99m,
                    StockQuantity = 100,
                    Sku = "SONYWH1000XM4",
                    Slug = "sony-wh-1000xm4",
                    MainImageUrl = "https://images.unsplash.com/photo-1484704849700-f032a568e944?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80",
                    IsOnSale = false,
                    IsFeatured = false,
                    IsNew = false,
                    IsActive = true,
                    CategoryId = 3,
                    CreatedAt = seedDate.AddDays(-30)
                },
                new Product
                {
                    Id = 4,
                    Name = "Apple Watch Series 8",
                    Description = "GPS + Cellular, Monitoreo de salud",
                    Price = 429.99m,
                    StockQuantity = 75,
                    Sku = "AWS8GPS",
                    Slug = "apple-watch-series-8",
                    MainImageUrl = "https://images.unsplash.com/photo-1546868871-7041f2a55e12?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80",
                    IsNew = true,
                    IsActive = true,
                    CategoryId = 4,
                    CreatedAt = seedDate.AddDays(-2)
                },
                new Product
                {
                    Id = 5,
                    Name = "Samsung Galaxy Tab S8",
                    Description = "Tablet Android de alta gama, S-Pen incluido",
                    Price = 699.99m,
                    OldPrice = 799.99m,
                    StockQuantity = 40,
                    Sku = "SGTABS8",
                    Slug = "samsung-galaxy-tab-s8",
                    MainImageUrl = "https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80",
                    IsOnSale = true,
                    IsFeatured = true,
                    IsNew = true,
                    IsActive = true,
                    CategoryId = 5,
                    CreatedAt = seedDate.AddDays(-7)
                },
                new Product
                {
                    Id = 6,
                    Name = "Cargador USB-C 65W",
                    Description = "Cargador rápido para laptop y teléfono",
                    Price = 29.99m,
                    StockQuantity = 200,
                    Sku = "CHARG65W",
                    Slug = "cargador-usb-c-65w",
                    MainImageUrl = "https://images.unsplash.com/photo-1594736797933-d0b64b5c45f1?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80",
                    IsOnSale = false,
                    IsFeatured = false,
                    IsNew = true,
                    IsActive = true,
                    CategoryId = 6,
                    CreatedAt = seedDate.AddDays(-1)
                }
            );
        }
    }
}
