using DnTech_Ecommerce.Models.Enums;

namespace DnTech_Ecommerce.ViewModels
{
    public class OrderConfirmationViewModel
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public OrderStatus Status { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus PaymentStatus { get; set; }

        // Información de envío
        public string ShippingFullName { get; set; } = string.Empty;
        public string ShippingEmail { get; set; } = string.Empty;
        public string ShippingPhone { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string ShippingCity { get; set; } = string.Empty;
        public string? ShippingState { get; set; }
        public string ShippingPostalCode { get; set; } = string.Empty;
        public string ShippingCountry { get; set; } = string.Empty;

        // Items del pedido
        public List<OrderItemViewModel> Items { get; set; } = new();

        // Totales
        public decimal Subtotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        public int TotalItems { get; set; }
    }
}
