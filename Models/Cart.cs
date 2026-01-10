using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DnTech_Ecommerce.Models
{
    public class Cart
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        [DataType(DataType.DateTime)]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Relación con items del carrito
        public virtual ICollection<CartItem> Items { get; set; } = new List<CartItem>();
        // Propiedades calculadas
        [NotMapped]
        public decimal Subtotal => Items.Sum(item => item.TotalPrice);

        [NotMapped]
        public int TotalItems => Items.Sum(item => item.Quantity);

        [NotMapped]
        public decimal ShippingCost => CalculateShipping();

        [NotMapped]
        public decimal Tax => Subtotal * 0.16m; // 16% IVA (ajustable)

        [NotMapped]
        public decimal Total => Subtotal + ShippingCost + Tax;

        private decimal CalculateShipping()
        {
            return Subtotal > 500 ? 0 : 50; // Envío gratis arriba de $500
        }
    }
}
