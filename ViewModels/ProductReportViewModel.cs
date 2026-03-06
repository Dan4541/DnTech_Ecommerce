namespace DnTech_Ecommerce.ViewModels
{
    public class ProductReportViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        
        // Totales
        public int TotalProductsSold { get; set; }
        public decimal TotalRevenue { get; set; }
        
        // Top productos
        public List<ProductReportItemViewModel> TopProducts { get; set; } = new List<ProductReportItemViewModel>();
        
        // Inventario actual
        public int LowStockProducts { get; set; }
        public int OutOfStockProducts { get; set; }
    }
    
    public class ProductReportItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public int CurrentStock { get; set; }
        public decimal UnitPrice { get; set; }
    }
}