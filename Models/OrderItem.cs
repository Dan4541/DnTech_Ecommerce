using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DnTech_Ecommerce.Models
{
    public class OrderItem
    {
        [Key]
        public int Id { get; set; }

        // Relación con Order
        [Required]
        public int OrderId { get; set; }
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        // Relación con Product
        [Required]
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        // Información del producto en el momento de la compra
        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? ProductSku { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        [Range(1, 100)]
        public int Quantity { get; set; }

        // Precio total del item
        [NotMapped]
        public decimal TotalPrice => Price * Quantity;
    }
}
