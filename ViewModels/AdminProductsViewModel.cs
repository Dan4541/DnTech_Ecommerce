using Microsoft.AspNetCore.Mvc.Rendering;

namespace DnTech_Ecommerce.ViewModels
{
    public class AdminProductsViewModel
    {
        // Lista de productos
        public List<AdminProductSummaryViewModel> Products { get; set; } = new List<AdminProductSummaryViewModel>();

        // Filtros
        public string? SearchTerm { get; set; }
        public int? FilterCategoryId { get; set; }
        public bool? FilterIsActive { get; set; }
        public bool? FilterLowStock { get; set; } // Stock < 10
        public bool? FilterOutOfStock { get; set; } // Stock = 0
        public bool? FilterIsFeatured { get; set; }
        public bool? FilterIsOnSale { get; set; }

        // Paginación
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 20;
        public int TotalProducts { get; set; }

        // Para dropdown de categorías
        public List<SelectListItem>? Categories { get; set; }

        // Estadísticas rápidas
        public int TotalActive { get; set; }
        public int TotalInactive { get; set; }
        public int TotalOutOfStock { get; set; }
        public int TotalLowStock { get; set; }
    }
}


