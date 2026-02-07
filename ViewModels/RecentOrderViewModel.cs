using DnTech_Ecommerce.Models.Enums;

namespace DnTech_Ecommerce.ViewModels
{
    public class RecentOrderViewModel
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; }
        public int ItemCount { get; set; }
    }
}
