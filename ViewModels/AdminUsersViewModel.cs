namespace DnTech_Ecommerce.ViewModels
{
    public class AdminUsersViewModel
    {
        // Lista de usuarios
        public List<AdminUserSummaryViewModel> Users { get; set; } = new List<AdminUserSummaryViewModel>();

        // Filtros
        public string? SearchTerm { get; set; }
        public string? FilterRole { get; set; }
        public bool? FilterIsActive { get; set; }

        // Paginación
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 20;
        public int TotalUsers { get; set; }

        // Estadísticas rápidas
        public int TotalAdministrators { get; set; }
        public int TotalClients { get; set; }
        public int TotalActive { get; set; }
        public int TotalInactive { get; set; }
    }
    
}
