using Microsoft.AspNetCore.Identity;

namespace DnTech_Ecommerce.Models
{
    public class User : IdentityUser
    {
        public string FullName { get; set; }
        public bool Active { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
