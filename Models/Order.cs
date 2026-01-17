using DnTech_Ecommerce.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DnTech_Ecommerce.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        // Número de orden único
        [Required]
        [StringLength(50)]
        public string OrderNumber { get; set; } = string.Empty;

        // Usuario
        [Required]
        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        // Información de envío
        [Required]
        [StringLength(100)]
        public string ShippingFullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string ShippingEmail { get; set; } = string.Empty;

        [Required]
        [Phone]
        [StringLength(20)]
        public string ShippingPhone { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string ShippingCity { get; set; } = string.Empty;

        [StringLength(50)]
        public string? ShippingState { get; set; }

        [Required]
        [StringLength(20)]
        public string ShippingPostalCode { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string ShippingCountry { get; set; } = string.Empty;

        // Montos
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingCost { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Tax { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        // Estado y pago
        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [Required]
        public PaymentMethod PaymentMethod { get; set; }

        [Required]
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        // Notas adicionales
        [StringLength(500)]
        public string? Notes { get; set; }

        // Fechas
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [DataType(DataType.DateTime)]
        public DateTime? ShippedDate { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? DeliveredDate { get; set; }

        // Relación con items
        public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

        // Propiedades calculadas
        [NotMapped]
        public int TotalItems => Items.Sum(item => item.Quantity);
    }
}
