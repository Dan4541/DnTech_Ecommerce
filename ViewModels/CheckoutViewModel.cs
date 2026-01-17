using DnTech_Ecommerce.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace DnTech_Ecommerce.ViewModels
{
    public class CheckoutViewModel
    {
        // Información de envío
        [Required(ErrorMessage = "El nombre completo es requerido")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        [Display(Name = "Nombre completo")]
        public string ShippingFullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        [Display(Name = "Email")]
        public string ShippingEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es requerido")]
        [Phone(ErrorMessage = "Teléfono inválido")]
        [Display(Name = "Teléfono")]
        public string ShippingPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "La dirección es requerida")]
        [StringLength(200, ErrorMessage = "La dirección no puede exceder 200 caracteres")]
        [Display(Name = "Dirección")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "La ciudad es requerida")]
        [StringLength(50, ErrorMessage = "La ciudad no puede exceder 50 caracteres")]
        [Display(Name = "Ciudad")]
        public string ShippingCity { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "El estado/provincia no puede exceder 50 caracteres")]
        [Display(Name = "Estado/Provincia")]
        public string? ShippingState { get; set; }

        [Required(ErrorMessage = "El código postal es requerido")]
        [StringLength(20, ErrorMessage = "El código postal no puede exceder 20 caracteres")]
        [Display(Name = "Código Postal")]
        public string ShippingPostalCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "El país es requerido")]
        [Display(Name = "País")]
        public string ShippingCountry { get; set; } = "Costa Rica";

        // Método de pago
        [Required(ErrorMessage = "Selecciona un método de pago")]
        [Display(Name = "Método de pago")]
        public PaymentMethod PaymentMethod { get; set; }

        // Notas adicionales (opcional)
        [StringLength(500, ErrorMessage = "Las notas no pueden exceder 500 caracteres")]
        [Display(Name = "Notas adicionales (opcional)")]
        public string? Notes { get; set; }

        // Resumen del carrito (solo lectura)
        public CartViewModel? Cart { get; set; }

        // Términos y condiciones
        [Required(ErrorMessage = "Debes aceptar los términos y condiciones")]
        [Display(Name = "Acepto los términos y condiciones")]
        public bool AcceptTerms { get; set; }
    }
}
