using DnTech_Ecommerce.Models.Enums;

namespace DnTech_Ecommerce.ViewModels
{
    public class AdminOrdersViewModel
    {
        // Lista de pedidos
        public List<AdminOrderSummaryViewModel> Orders { get; set; } = new List<AdminOrderSummaryViewModel>();

        // Filtros
        public string? SearchTerm { get; set; }
        public OrderStatus? FilterStatus { get; set; }
        public DateTime? FilterDateFrom { get; set; }
        public DateTime? FilterDateTo { get; set; }
        public decimal? FilterMinAmount { get; set; }
        public decimal? FilterMaxAmount { get; set; }

        // Paginación
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 20;
        public int TotalOrders { get; set; }

        // Estadísticas rápidas
        public int TotalPending { get; set; }
        public int TotalProcessing { get; set; }
        public int TotalShipped { get; set; }
        public int TotalDelivered { get; set; }
        public int TotalCancelled { get; set; }
    }
}
