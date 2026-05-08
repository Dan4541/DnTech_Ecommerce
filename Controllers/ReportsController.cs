using DnTech_Ecommerce.Services;
using DnTech_Ecommerce.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DnTech_Ecommerce.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly ReportService _reportService;

        public ReportsController(ReportService reportService)
        {
            _reportService = reportService;
        }

        // GET: /Reports
        public IActionResult Index()
        {
            ViewData["Title"] = "Reportes";
            ViewData["Breadcrumbs"] = @"
                <li class='breadcrumb-item'><a asp-controller='Admin' asp-action='Dashboard'>Dashboard</a></li>
                <li class='breadcrumb-item active'>Reportes</li>";

            return View();
        }

        // GET: /Reports/SalesReport
        public async Task<IActionResult> SalesReport(ReportFilterViewModel filter)
        {
            // Establecer fechas por defecto según período
            SetDefaultDates(filter);

            var report = await _reportService.GenerateSalesReportData(filter.StartDate!.Value, filter.EndDate!.Value);

            ViewData["Title"] = "Reporte de Ventas";
            ViewData["Breadcrumbs"] = @"
                <li class='breadcrumb-item'><a asp-controller='Admin' asp-action='Dashboard'>Dashboard</a></li>
                <li class='breadcrumb-item'><a asp-controller='Reports' asp-action='Index'>Reportes</a></li>
                <li class='breadcrumb-item active'>Ventas</li>";

            ViewBag.Filter = filter;
            return View(report);
        }

        // GET: /Reports/ProductReport
        public async Task<IActionResult> ProductReport(ReportFilterViewModel filter)
        {
            SetDefaultDates(filter);

            var report = await _reportService.GenerateProductReportData(filter.StartDate!.Value, filter.EndDate!.Value);

            ViewData["Title"] = "Reporte de Productos";
            ViewData["Breadcrumbs"] = @"
                <li class='breadcrumb-item'><a asp-controller='Admin' asp-action='Dashboard'>Dashboard</a></li>
                <li class='breadcrumb-item'><a asp-controller='Reports' asp-action='Index'>Reportes</a></li>
                <li class='breadcrumb-item active'>Productos</li>";

            ViewBag.Filter = filter;
            return View(report);
        }

        // GET: /Reports/ExportSalesExcel
        public async Task<IActionResult> ExportSalesExcel(DateTime startDate, DateTime endDate)
        {
            try
            {
                var report = await _reportService.GenerateSalesReportData(startDate, endDate);
                var excelData = await _reportService.ExportSalesReportToExcel(report);

                var fileName = $"Reporte_Ventas_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.xlsx";
                return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al generar reporte: {ex.Message}";
                return RedirectToAction("SalesReport");
            }
        }

        // GET: /Reports/ExportSalesPDF
        public async Task<IActionResult> ExportSalesPDF(DateTime startDate, DateTime endDate)
        {
            try
            {
                var report = await _reportService.GenerateSalesReportData(startDate, endDate);
                var pdfData = await _reportService.ExportSalesReportToPDF(report);

                var fileName = $"Reporte_Ventas_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.pdf";
                return File(pdfData, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al generar reporte: {ex.Message}";
                return RedirectToAction("SalesReport");
            }
        }

        // GET: /Reports/ExportProductExcel
        public async Task<IActionResult> ExportProductExcel(DateTime startDate, DateTime endDate)
        {
            try
            {
                var report = await _reportService.GenerateProductReportData(startDate, endDate);
                var excelData = await _reportService.ExportProductReportToExcel(report);

                var fileName = $"Reporte_Productos_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.xlsx";
                return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al generar reporte: {ex.Message}";
                return RedirectToAction("ProductReport");
            }
        }

        // GET: /Reports/ExportProductPDF
        public async Task<IActionResult> ExportProductPDF(DateTime startDate, DateTime endDate)
        {
            try
            {
                var report = await _reportService.GenerateProductReportData(startDate, endDate);
                var pdfData = await _reportService.ExportProductReportToPDF(report);

                var fileName = $"Reporte_Productos_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.pdf";
                return File(pdfData, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al generar reporte: {ex.Message}";
                return RedirectToAction("ProductReport");
            }
        }

        // Método auxiliar para establecer fechas por defecto
        private void SetDefaultDates(ReportFilterViewModel filter)
        {
            var today = DateTime.Today;

            if (filter.StartDate == null || filter.EndDate == null)
            {
                switch (filter.Period)
                {
                    case "today":
                        filter.StartDate = today;
                        filter.EndDate = today;
                        break;
                    case "week":
                        filter.StartDate = today.AddDays(-(int)today.DayOfWeek);
                        filter.EndDate = today;
                        break;
                    case "month":
                        filter.StartDate = new DateTime(today.Year, today.Month, 1);
                        filter.EndDate = today;
                        break;
                    default:
                        // Por defecto, último mes
                        filter.StartDate = today.AddMonths(-1);
                        filter.EndDate = today;
                        break;
                }
            }
        }
    }
}