using System.ComponentModel.DataAnnotations;

namespace DnTech_Ecommerce.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, ErrorMessage = "{0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required")]
        [StringLength(200, ErrorMessage = "La dirección no puede exceder 200 caracteres")]
        [Display(Name = "Address")]
        public string Address { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "The name of the city cannot exceed 50 characters long.")]
        [Display(Name = "City")]
        public string? City { get; set; }

        [StringLength(10, ErrorMessage = "El código postal no puede exceder 10 caracteres")]
        [Display(Name = "Postal Code")]
        public string? PostalCode { get; set; }

        [StringLength(50, ErrorMessage = "El país no puede exceder 50 caracteres")]
        [Display(Name = "Country")]
        public string Country { get; set; } = "México";

        [Display(Name = "Phone")]
        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Role")]
        public string? SelectedRole { get; set; }
    }
}
