using System.ComponentModel.DataAnnotations;

namespace DnTech_Ecommerce.ViewModels
{
    public class AddToCartViewModel
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, 100, ErrorMessage = "La cantidad debe ser entre 1 y 100")]
        public int Quantity { get; set; } = 1;
    }
}
