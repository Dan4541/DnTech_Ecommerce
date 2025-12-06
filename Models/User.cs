using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace DnTech_Ecommerce.Models
{
    public class User : IdentityUser
    {
        public string FullName { get; set; }

        public string? Address { get; set; }

        [StringLength(50)]
        public string? City { get; set; }

        [StringLength(10)]
        public string? PostalCode { get; set; }

        [StringLength(50)]
        public string? Country { get; set; }

        public bool Active { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
