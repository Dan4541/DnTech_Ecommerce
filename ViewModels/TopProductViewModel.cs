namespace DnTech_Ecommerce.ViewModels
{
    public class TopProductViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int TotalSold { get; set; }
        public decimal Revenue { get; set; }
        public int CurrentStock { get; set; }
    }
}
