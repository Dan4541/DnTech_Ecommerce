using Microsoft.AspNetCore.Identity;

namespace DnTech_Ecommerce.Models
{
    public class Role : IdentityRole
    {
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
