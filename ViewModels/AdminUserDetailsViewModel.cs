using Microsoft.AspNetCore.Mvc.Rendering;

namespace DnTech_Ecommerce.ViewModels
{
    public class AdminUserDetailsViewModel
    {
        // Información del usuario
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public bool Active { get; set; }
        public DateTime CreatedAt { get; set; }

        // Roles
        public List<string> CurrentRoles { get; set; } = new List<string>();
        public List<SelectListItem> AvailableRoles { get; set; } = new List<SelectListItem>();

        // Estadísticas
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public int PendingOrders { get; set; }
        public int CompletedOrders { get; set; }

        // Historial de pedidos
        public List<UserOrderHistoryViewModel> RecentOrders { get; set; } = new List<UserOrderHistoryViewModel>();
    }

    public class UserOrderHistoryViewModel
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}