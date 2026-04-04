using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DnTech_Ecommerce.Models
{
    
    public class Notification
    {
        [Key]
        public int Id { get; set; }
 
        // Usuario destinatario
        [Required]
        public string UserId { get; set; } = string.Empty;
 
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
 
        // Contenido de la notificación
        [Required]
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;
 
        // Tipo de notificación
        [Required]
        public NotificationType Type { get; set; }
 
        // Link relacionado (opcional)
        [StringLength(200)]
        public string? Link { get; set; }
 
        // ID del pedido relacionado (si aplica)
        public int? OrderId { get; set; }
 
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }
 
        // Estado
        public bool IsRead { get; set; } = false;
 
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
 
        [DataType(DataType.DateTime)]
        public DateTime? ReadAt { get; set; }
    }

    // Enum para tipos de notificaciones
    public enum NotificationType
    {
        NewOrder,           // Nuevo pedido (para admin)
        OrderStatusChange,  // Cambio de estado de pedido (para cliente)
        OrderShipped,       // Pedido enviado (para cliente)
        OrderDelivered,     // Pedido entregado (para cliente)
        OrderCancelled,     // Pedido cancelado (para cliente)
        PaymentConfirmed,   // Pago confirmado (para cliente)
        System,             // Notificación del sistema
        Other               // Otros
    }

}
