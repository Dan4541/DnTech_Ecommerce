using System.ComponentModel.DataAnnotations;

namespace DnTech_Ecommerce.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        [Display(Name = "Nombre")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        [Display(Name = "Descripción")]
        public string? Description { get; set; }

        [StringLength(100)]
        [Display(Name = "Slug (URL)")]
        public string? Slug { get; set; }

        [Display(Name = "Imagen")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Activa")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Fecha de Creación")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Fecha de Actualización")]
        public DateTime? UpdatedAt { get; set; }

        // Relación con productos
        public virtual ICollection<Product>? Products { get; set; }
    }
}
