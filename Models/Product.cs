using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DnTech_Ecommerce.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
        [Display(Name = "Nombre")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "La descripción no puede exceder 1000 caracteres")]
        [Display(Name = "Descripción")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "El precio es requerido")]
        [Range(0.01, 1000000, ErrorMessage = "El precio debe ser mayor a 0")]
        [DataType(DataType.Currency)]
        [Display(Name = "Precio")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Display(Name = "Precio Anterior")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? OldPrice { get; set; }

        [Required(ErrorMessage = "La cantidad en stock es requerida")]
        [Range(0, 10000, ErrorMessage = "La cantidad debe ser entre 0 y 10000")]
        [Display(Name = "Stock")]
        public int StockQuantity { get; set; }

        [StringLength(100)]
        [Display(Name = "SKU (Código)")]
        public string? Sku { get; set; }

        [StringLength(200)]
        [Display(Name = "Slug (URL)")]
        public string? Slug { get; set; }

        [Display(Name = "Imagen Principal")]
        public string? MainImageUrl { get; set; }

        [Display(Name = "Imágenes Adicionales")]
        public string? AdditionalImages { get; set; } // JSON o string separado por comas

        [Display(Name = "En Oferta")]
        public bool IsOnSale { get; set; } = false;

        [Display(Name = "Destacado")]
        public bool IsFeatured { get; set; } = false;

        [Display(Name = "Nuevo")]
        public bool IsNew { get; set; } = true;

        [Display(Name = "Activo")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Calificación")]
        [Range(0, 5, ErrorMessage = "La calificación debe ser entre 0 y 5")]
        public decimal Rating { get; set; } = 0;

        [Display(Name = "Número de Reseñas")]
        public int ReviewCount { get; set; } = 0;

        [Display(Name = "Fecha de Creación")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Fecha de Actualización")]
        public DateTime? UpdatedAt { get; set; }

        // Relación con categoría
        [Display(Name = "Categoría")]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }
    }
}
