using DnTech_Ecommerce.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace DnTech_Ecommerce.ViewModels
{
    public class ProductViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
        [Display(Name = "Nombre")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "La descripción es requerida")]
        [StringLength(1000, ErrorMessage = "La descripción no puede exceder 1000 caracteres")]
        [Display(Name = "Descripción")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "El precio es requerido")]
        [Range(0.01, 1000000, ErrorMessage = "El precio debe ser mayor a 0")]
        [DataType(DataType.Currency)]
        [Display(Name = "Precio")]
        public decimal Price { get; set; }

        [DataType(DataType.Currency)]
        [Display(Name = "Precio Anterior (para ofertas)")]
        public decimal? OldPrice { get; set; }

        [Required(ErrorMessage = "La cantidad en stock es requerida")]
        [Range(0, 10000, ErrorMessage = "La cantidad debe ser entre 0 y 10000")]
        [Display(Name = "Stock")]
        public int StockQuantity { get; set; }

        [StringLength(100)]
        [Display(Name = "SKU (Código)")]
        public string? Sku { get; set; }

        [Display(Name = "Imagen Principal")]
        public IFormFile? MainImage { get; set; }

        public string? MainImageUrl { get; set; }

        [Display(Name = "En Oferta")]
        public bool IsOnSale { get; set; }

        [Display(Name = "Destacado")]
        public bool IsFeatured { get; set; }

        [Display(Name = "Nuevo")]
        public bool IsNew { get; set; } = true;

        [Display(Name = "Activo")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Categoría")]
        public int CategoryId { get; set; }

        // Para el dropdown de categorías
        public List<SelectListItem>? Categories { get; set; }

        public List<Product>? Products { get; set; }

        // Propiedades para filtros y búsqueda
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; } = "newest";
    }
}
