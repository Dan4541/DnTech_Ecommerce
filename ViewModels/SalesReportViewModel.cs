using DnTech_Ecommerce.Models;
using DnTech_Ecommerce.Models.Enums;

namespace DnTech_Ecommerce.ViewModels
{
    public class SalesReportViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        
        // Totales generales
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int TotalItemsSold { get; set; }
        
        // Por estado
        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int ShippedOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CancelledOrders { get; set; }
        
        // Detalles de pedidos
        public List<SalesReportOrderViewModel> Orders { get; set; } = new List<SalesReportOrderViewModel>();
        
        // Ventas por día (para gráfico)
        public Dictionary<DateTime, decimal> DailySales { get; set; } = new Dictionary<DateTime, decimal>();
    }
    
    public class SalesReportOrderViewModel
    {
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public int ItemCount { get; set; }
    }
}