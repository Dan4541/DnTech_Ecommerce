using DnTech_Ecommerce.Models.Enums;

namespace DnTech_Ecommerce.ViewModels
{
    public class OrderSummaryViewModel
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public OrderStatus Status { get; set; }
        public decimal Total { get; set; }
        public int TotalItems { get; set; }
        public string StatusDisplay => Status switch
        {
            OrderStatus.Pending => "Pendiente",
            OrderStatus.Processing => "Procesando",
            OrderStatus.Shipped => "Enviado",
            OrderStatus.Delivered => "Entregado",
            OrderStatus.Cancelled => "Cancelado",
            _ => "Desconocido"
        };
        public string StatusColor => Status switch
        {
            OrderStatus.Pending => "warning",
            OrderStatus.Processing => "info",
            OrderStatus.Shipped => "primary",
            OrderStatus.Delivered => "success",
            OrderStatus.Cancelled => "danger",
            _ => "secondary"
        };
    }
}
