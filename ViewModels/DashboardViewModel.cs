namespace DnTech_Ecommerce.ViewModels
{
    public class DashboardViewModel
    {
        // Métricas de Pedidos
        public int TotalOrdersToday { get; set; }
        public int TotalOrdersWeek { get; set; }
        public int TotalOrdersMonth { get; set; }
        public int PendingOrders { get; set; }

        // Métricas de Ingresos
        public decimal RevenueToday { get; set; }
        public decimal RevenueWeek { get; set; }
        public decimal RevenueMonth { get; set; }

        // Métricas de Productos
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public int LowStockProducts { get; set; } // Stock < 10

        // Métricas de Usuarios
        public int TotalUsers { get; set; }
        public int NewUsersThisMonth { get; set; }

        // Listas
        public List<RecentOrderViewModel> RecentOrders { get; set; } = new List<RecentOrderViewModel>();
        public List<TopProductViewModel> TopProducts { get; set; } = new List<TopProductViewModel>();

        // Cambios porcentuales (comparación con período anterior)
        public decimal OrdersChangePercent { get; set; }
        public decimal RevenueChangePercent { get; set; }
    }
}
