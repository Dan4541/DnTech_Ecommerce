namespace DnTech_Ecommerce.ViewModels
{
    public class ReportFilterViewModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string ReportType { get; set; } = "sales"; // sales, products, customers, inventory
        public string Period { get; set; } = "month"; // today, week, month, custom
    }
}