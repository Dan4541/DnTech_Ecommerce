using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DnTech_Ecommerce.Migrations
{
    /// <inheritdoc />
    public partial class CreateProductAndCategoryTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OldPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    StockQuantity = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Sku = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MainImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AdditionalImages = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsOnSale = table.Column<bool>(type: "bit", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    IsNew = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Rating = table.Column<decimal>(type: "decimal(3,2)", nullable: false, defaultValue: 0m),
                    ReviewCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "CreatedAt", "Description", "ImageUrl", "IsActive", "Name", "Slug", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Teléfonos inteligentes", null, true, "Smartphones", "smartphones", null },
                    { 2, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Computadoras portátiles", null, true, "Laptops", "laptops", null },
                    { 3, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Auriculares y altavoces", null, true, "Audio", "audio", null },
                    { 4, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Dispositivos portátiles", null, true, "Wearables", "wearables", null },
                    { 5, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Tabletas electrónicas", null, true, "Tablets", "tablets", null },
                    { 6, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Accesorios para dispositivos", null, true, "Accesorios", "accesorios", null }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "AdditionalImages", "CategoryId", "CreatedAt", "Description", "IsActive", "IsFeatured", "IsNew", "IsOnSale", "MainImageUrl", "Name", "OldPrice", "Price", "ReviewCount", "Sku", "Slug", "StockQuantity", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, null, 1, new DateTime(2024, 12, 22, 0, 0, 0, 0, DateTimeKind.Utc), "256GB, Pantalla Dynamic Island, Cámara profesional", true, true, true, true, "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80", "iPhone 14 Pro", 1199.99m, 999.99m, 0, "IPH14P256", "iphone-14-pro", 50, null },
                    { 2, null, 2, new DateTime(2024, 12, 27, 0, 0, 0, 0, DateTimeKind.Utc), "16GB RAM, 512GB SSD, Chip M2", true, true, true, false, "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80", "MacBook Pro M2", null, 1299.99m, 0, "MBPM216512", "macbook-pro-m2", 30, null },
                    { 3, null, 3, new DateTime(2024, 12, 2, 0, 0, 0, 0, DateTimeKind.Utc), "Auriculares con cancelación de ruido", true, false, false, false, "https://images.unsplash.com/photo-1484704849700-f032a568e944?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80", "Sony WH-1000XM4", null, 349.99m, 0, "SONYWH1000XM4", "sony-wh-1000xm4", 100, null },
                    { 4, null, 4, new DateTime(2024, 12, 30, 0, 0, 0, 0, DateTimeKind.Utc), "GPS + Cellular, Monitoreo de salud", true, false, true, false, "https://images.unsplash.com/photo-1546868871-7041f2a55e12?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80", "Apple Watch Series 8", null, 429.99m, 0, "AWS8GPS", "apple-watch-series-8", 75, null },
                    { 5, null, 5, new DateTime(2024, 12, 25, 0, 0, 0, 0, DateTimeKind.Utc), "Tablet Android de alta gama, S-Pen incluido", true, true, true, true, "https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80", "Samsung Galaxy Tab S8", 799.99m, 699.99m, 0, "SGTABS8", "samsung-galaxy-tab-s8", 40, null },
                    { 6, null, 6, new DateTime(2024, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Cargador rápido para laptop y teléfono", true, false, true, false, "https://images.unsplash.com/photo-1594736797933-d0b64b5c45f1?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80", "Cargador USB-C 65W", null, 29.99m, 0, "CHARG65W", "cargador-usb-c-65w", 200, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug",
                table: "Categories",
                column: "Slug",
                unique: true,
                filter: "[Slug] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsActive",
                table: "Products",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsFeatured",
                table: "Products",
                column: "IsFeatured");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsOnSale",
                table: "Products",
                column: "IsOnSale");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Name",
                table: "Products",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Sku",
                table: "Products",
                column: "Sku",
                unique: true,
                filter: "[Sku] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Slug",
                table: "Products",
                column: "Slug",
                unique: true,
                filter: "[Slug] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
