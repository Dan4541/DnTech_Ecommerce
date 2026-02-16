namespace DnTech_Ecommerce.ViewModels
{
    public class AdminCategoriesViewModel
    {
        // Lista de categorías
        public List<AdminCategorySummaryViewModel> Categories { get; set; } = new List<AdminCategorySummaryViewModel>();

        // Estadísticas
        public int TotalCategories { get; set; }
        public int ActiveCategories { get; set; }
        public int InactiveCategories { get; set; }
    }    
}