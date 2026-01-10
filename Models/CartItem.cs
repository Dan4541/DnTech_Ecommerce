using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DnTech_Ecommerce.Models
{
    public class CartItem
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int CartId { get; set; }
        [Required]
        public int ProductId { get; set; }
        [Required]
        [Range(1, 100, ErrorMessage = "La cantidad debe ser entre 1 y 100")]
        public int Quantity { get; set; } = 1;

        [DataType(DataType.DateTime)]
        public DateTime AddedAt { get; set; } = DateTime.Now;

        // Precio en el momento de agregar al carrito
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        // Relaciones
        [ForeignKey("CartId")]
        public virtual Cart? Cart { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        // Propiedades calculadas
        [NotMapped]
        public decimal TotalPrice => Price * Quantity;

        [NotMapped]
        public decimal? OldPrice => Product?.OldPrice;
    }
}
