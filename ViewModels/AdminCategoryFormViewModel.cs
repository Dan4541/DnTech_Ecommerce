using System.ComponentModel.DataAnnotations;

namespace DnTech_Ecommerce.ViewModels
{
    public class AdminCategoryFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }
}