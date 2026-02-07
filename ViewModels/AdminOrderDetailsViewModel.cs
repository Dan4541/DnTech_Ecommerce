using DnTech_Ecommerce.Models.Enums;

namespace DnTech_Ecommerce.ViewModels
{
    public class AdminOrderDetailsViewModel
    {
        // Información del pedido
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public OrderStatus Status { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus PaymentStatus { get; set; }

        // Información del cliente
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;

        // Dirección de envío
        public string ShippingAddress { get; set; } = string.Empty;
        public string ShippingCity { get; set; } = string.Empty;
        public string ShippingPostalCode { get; set; } = string.Empty;
        public string ShippingCountry { get; set; } = string.Empty;

        // Items del pedido
        public List<AdminOrderItemViewModel> Items { get; set; } = new List<AdminOrderItemViewModel>();

        // Totales
        public decimal SubTotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal Tax { get; set; }
        public decimal TotalAmount { get; set; }

        // Notas
        public string? Notes { get; set; }

        // Información de cancelación
        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }
    }
}
