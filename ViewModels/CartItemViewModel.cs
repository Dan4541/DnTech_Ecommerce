namespace DnTech_Ecommerce.ViewModels
{
    public class CartItemViewModel
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductImage { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? OldPrice { get; set; }
        public int Quantity { get; set; }
        public int MaxStock { get; set; }
        public decimal TotalPrice { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
    }
}
