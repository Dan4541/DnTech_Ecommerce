using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DnTech_Ecommerce.Migrations
{
    /// <inheritdoc />
    public partial class ReorderPaymentStatusEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE Orders 
                SET PaymentStatus = CASE 
                    WHEN PaymentStatus = 3 THEN 1  -- Completed: de 3 a 1
                    WHEN PaymentStatus = 1 THEN 2  -- Failed: de 1 a 2
                    WHEN PaymentStatus = 2 THEN 3  -- Refunded: de 2 a 3
                    ELSE PaymentStatus 
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revertir cambios si haces rollback
            migrationBuilder.Sql(@"
                UPDATE Orders 
                SET PaymentStatus = CASE 
                    WHEN PaymentStatus = 1 THEN 3  -- Completed: de 1 a 3
                    WHEN PaymentStatus = 2 THEN 1  -- Failed: de 2 a 1
                    WHEN PaymentStatus = 3 THEN 2  -- Refunded: de 3 a 2
                    ELSE PaymentStatus 
                END
            ");
        }
    }
}
