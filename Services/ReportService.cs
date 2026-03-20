using DnTech_Ecommerce.Data;
using DnTech_Ecommerce.Models;
using DnTech_Ecommerce.Models.Enums;
using DnTech_Ecommerce.ViewModels;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DnTech_Ecommerce.Services
{
    public class ReportService
    {
        private readonly ApplicationDbContext _context;

        
        static ReportService()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;            
        }
        

        public ReportService(ApplicationDbContext context)
        {
            _context = context;            
            QuestPDF.Settings.License = LicenseType.Community;
        }

        // ============================================
        // GENERAR DATOS DE REPORTES
        // ============================================

        public async Task<SalesReportViewModel> GenerateSalesReportData(DateTime startDate, DateTime endDate)
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Items)
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var report = new SalesReportViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalOrders = orders.Count,
                TotalRevenue = orders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.Total),
                AverageOrderValue = orders.Any() ? orders.Where(o => o.Status != OrderStatus.Cancelled).Average(o => o.Total) : 0,
                TotalItemsSold = orders.SelectMany(o => o.Items).Sum(oi => oi.Quantity),
                PendingOrders = orders.Count(o => o.Status == OrderStatus.Pending),
                ProcessingOrders = orders.Count(o => o.Status == OrderStatus.Processing),
                ShippedOrders = orders.Count(o => o.Status == OrderStatus.Shipped),
                DeliveredOrders = orders.Count(o => o.Status == OrderStatus.Delivered),
                CancelledOrders = orders.Count(o => o.Status == OrderStatus.Cancelled),
                Orders = orders.Select(o => new SalesReportOrderViewModel
                {
                    OrderNumber = o.OrderNumber,
                    OrderDate = o.OrderDate,
                    CustomerName = o.User.FullName,
                    CustomerEmail = o.User.Email,
                    TotalAmount = o.Total,
                    Status = o.Status,
                    PaymentMethod = o.PaymentMethod,
                    ItemCount = o.Items.Count
                }).ToList()
            };

            // Ventas por día para gráfico
            report.DailySales = orders
                .Where(o => o.Status != OrderStatus.Cancelled)
                .GroupBy(o => o.OrderDate.Date)
                .ToDictionary(g => g.Key, g => g.Sum(o => o.Total));

            return report;
        }

        public async Task<ProductReportViewModel> GenerateProductReportData(DateTime startDate, DateTime endDate)
        {
            var orderItems = await _context.OrderItems
                .Include(oi => oi.Product)
                    .ThenInclude(p => p.Category)
                .Include(oi => oi.Order)
                .Where(oi => oi.Order.OrderDate >= startDate && 
                            oi.Order.OrderDate <= endDate &&
                            oi.Order.Status != OrderStatus.Cancelled)
                .ToListAsync();

            var topProducts = orderItems
                .GroupBy(oi => new
                {
                    oi.ProductId,
                    oi.Product.Name,
                    oi.Product.Sku,
                    CategoryName = oi.Product.Category != null ? oi.Product.Category.Name : "Sin categoría",
                    oi.Product.StockQuantity,
                    oi.Product.Price
                })
                .Select(g => new ProductReportItemViewModel
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.Name,
                    Sku = g.Key.Sku,
                    CategoryName = g.Key.CategoryName,
                    QuantitySold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.Price * oi.Quantity),
                    CurrentStock = g.Key.StockQuantity,
                    UnitPrice = g.Key.Price
                })
                .OrderByDescending(p => p.QuantitySold)
                .ToList();

            var allProducts = await _context.Products.ToListAsync();

            var report = new ProductReportViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalProductsSold = orderItems.Sum(oi => oi.Quantity),
                TotalRevenue = orderItems.Sum(oi => oi.Price * oi.Quantity),
                TopProducts = topProducts,
                LowStockProducts = allProducts.Count(p => p.StockQuantity > 0 && p.StockQuantity < 10),
                OutOfStockProducts = allProducts.Count(p => p.StockQuantity == 0)
            };

            return report;
        }

        // ============================================
        // EXPORTAR A EXCEL
        // ============================================

        // MÉTODO 1: ExportSalesReportToExcel        

        public async Task<byte[]> ExportSalesReportToExcel(SalesReportViewModel report)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Reporte de Ventas");

            // Título
            worksheet.Cells["A1"].Value = "REPORTE DE VENTAS";
            worksheet.Cells["A1:G1"].Merge = true;
            worksheet.Cells["A1"].Style.Font.Size = 16;
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Período
            worksheet.Cells["A2"].Value = $"Período: {report.StartDate:dd/MM/yyyy} - {report.EndDate:dd/MM/yyyy}";
            worksheet.Cells["A2:G2"].Merge = true;
            worksheet.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Resumen
            int row = 4;
            worksheet.Cells[$"A{row}"].Value = "RESUMEN GENERAL";
            worksheet.Cells[$"A{row}"].Style.Font.Bold = true;
            row++;

            worksheet.Cells[$"A{row}"].Value = "Total de Pedidos:";
            worksheet.Cells[$"B{row}"].Value = report.TotalOrders;
            row++;

            worksheet.Cells[$"A{row}"].Value = "Ingresos Totales:";
            worksheet.Cells[$"B{row}"].Value = report.TotalRevenue;
            worksheet.Cells[$"B{row}"].Style.Numberformat.Format = "₡#,##0.00";
            row++;

            worksheet.Cells[$"A{row}"].Value = "Valor Promedio por Pedido:";
            worksheet.Cells[$"B{row}"].Value = report.AverageOrderValue;
            worksheet.Cells[$"B{row}"].Style.Numberformat.Format = "₡#,##0.00";
            row++;

            worksheet.Cells[$"A{row}"].Value = "Total de Productos Vendidos:";
            worksheet.Cells[$"B{row}"].Value = report.TotalItemsSold;
            row += 2;

            // Estados
            worksheet.Cells[$"A{row}"].Value = "POR ESTADO";
            worksheet.Cells[$"A{row}"].Style.Font.Bold = true;
            row++;

            worksheet.Cells[$"A{row}"].Value = "Pendientes:";
            worksheet.Cells[$"B{row}"].Value = report.PendingOrders;
            row++;

            worksheet.Cells[$"A{row}"].Value = "Procesando:";
            worksheet.Cells[$"B{row}"].Value = report.ProcessingOrders;
            row++;

            worksheet.Cells[$"A{row}"].Value = "Enviados:";
            worksheet.Cells[$"B{row}"].Value = report.ShippedOrders;
            row++;

            worksheet.Cells[$"A{row}"].Value = "Entregados:";
            worksheet.Cells[$"B{row}"].Value = report.DeliveredOrders;
            row++;

            worksheet.Cells[$"A{row}"].Value = "Cancelados:";
            worksheet.Cells[$"B{row}"].Value = report.CancelledOrders;
            row += 2;

            // Tabla de pedidos
            worksheet.Cells[$"A{row}"].Value = "DETALLE DE PEDIDOS";
            worksheet.Cells[$"A{row}"].Style.Font.Bold = true;
            row++;

            // Encabezados
            worksheet.Cells[$"A{row}"].Value = "Número";
            worksheet.Cells[$"B{row}"].Value = "Fecha";
            worksheet.Cells[$"C{row}"].Value = "Cliente";
            worksheet.Cells[$"D{row}"].Value = "Email";
            worksheet.Cells[$"E{row}"].Value = "Items";
            worksheet.Cells[$"F{row}"].Value = "Total";
            worksheet.Cells[$"G{row}"].Value = "Estado";
            
            // Estilo de encabezados (sin System.Drawing.Color)
            var headerRange = worksheet.Cells[$"A{row}:G{row}"];
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            headerRange.Style.Fill.BackgroundColor.SetColor(211, 211, 211, 255); // RGB para LightGray
            row++;

            // Datos
            foreach (var order in report.Orders)
            {
                worksheet.Cells[$"A{row}"].Value = order.OrderNumber;
                worksheet.Cells[$"B{row}"].Value = order.OrderDate.ToString("dd/MM/yyyy HH:mm");
                worksheet.Cells[$"C{row}"].Value = order.CustomerName;
                worksheet.Cells[$"D{row}"].Value = order.CustomerEmail;
                worksheet.Cells[$"E{row}"].Value = order.ItemCount;
                worksheet.Cells[$"F{row}"].Value = order.TotalAmount;
                worksheet.Cells[$"F{row}"].Style.Numberformat.Format = "₡#,##0.00";
                worksheet.Cells[$"G{row}"].Value = order.Status.ToString();
                row++;
            }

            // Ajustar columnas
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            return await Task.FromResult(package.GetAsByteArray());
        }

        // ============================================

        public async Task<byte[]> ExportProductReportToExcel(ProductReportViewModel report)
        {

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Reporte de Productos");

            // Título
            worksheet.Cells["A1"].Value = "REPORTE DE PRODUCTOS MÁS VENDIDOS";
            worksheet.Cells["A1:H1"].Merge = true;
            worksheet.Cells["A1"].Style.Font.Size = 16;
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Período
            worksheet.Cells["A2"].Value = $"Período: {report.StartDate:dd/MM/yyyy} - {report.EndDate:dd/MM/yyyy}";
            worksheet.Cells["A2:H2"].Merge = true;
            worksheet.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Resumen
            int row = 4;
            worksheet.Cells[$"A{row}"].Value = "RESUMEN GENERAL";
            worksheet.Cells[$"A{row}"].Style.Font.Bold = true;
            row++;

            worksheet.Cells[$"A{row}"].Value = "Total de Productos Vendidos:";
            worksheet.Cells[$"B{row}"].Value = report.TotalProductsSold;
            row++;

            worksheet.Cells[$"A{row}"].Value = "Ingresos Totales:";
            worksheet.Cells[$"B{row}"].Value = report.TotalRevenue;
            worksheet.Cells[$"B{row}"].Style.Numberformat.Format = "₡#,##0.00";
            row++;

            worksheet.Cells[$"A{row}"].Value = "Productos con Stock Bajo:";
            worksheet.Cells[$"B{row}"].Value = report.LowStockProducts;
            row++;

            worksheet.Cells[$"A{row}"].Value = "Productos Sin Stock:";
            worksheet.Cells[$"B{row}"].Value = report.OutOfStockProducts;
            row += 2;

            // Tabla de productos
            worksheet.Cells[$"A{row}"].Value = "TOP PRODUCTOS";
            worksheet.Cells[$"A{row}"].Style.Font.Bold = true;
            row++;

            // Encabezados
            worksheet.Cells[$"A{row}"].Value = "SKU";
            worksheet.Cells[$"B{row}"].Value = "Producto";
            worksheet.Cells[$"C{row}"].Value = "Categoría";
            worksheet.Cells[$"D{row}"].Value = "Cantidad Vendida";
            worksheet.Cells[$"E{row}"].Value = "Precio Unitario";
            worksheet.Cells[$"F{row}"].Value = "Ingresos";
            worksheet.Cells[$"G{row}"].Value = "Stock Actual";    

            var headerRange = worksheet.Cells[$"A{row}:G{row}"];
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            headerRange.Style.Fill.BackgroundColor.SetColor(211, 211, 211, 255); // RGB para LightGray
            row++;

            // Datos
            foreach (var product in report.TopProducts)
            {
                worksheet.Cells[$"A{row}"].Value = product.Sku ?? "-";
                worksheet.Cells[$"B{row}"].Value = product.ProductName;
                worksheet.Cells[$"C{row}"].Value = product.CategoryName;
                worksheet.Cells[$"D{row}"].Value = product.QuantitySold;
                worksheet.Cells[$"E{row}"].Value = product.UnitPrice;
                worksheet.Cells[$"E{row}"].Style.Numberformat.Format = "₡#,##0.00";
                worksheet.Cells[$"F{row}"].Value = product.Revenue;
                worksheet.Cells[$"F{row}"].Style.Numberformat.Format = "₡#,##0.00";
                worksheet.Cells[$"G{row}"].Value = product.CurrentStock;
                row++;
            }

            // Ajustar columnas
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            return await Task.FromResult(package.GetAsByteArray());
        }

        // ============================================
        // EXPORTAR A PDF
        // ============================================

        public async Task<byte[]> ExportSalesReportToPDF(SalesReportViewModel report)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);

                    // Header
                    page.Header().Column(column =>
                    {
                        column.Item().AlignCenter().Text("REPORTE DE VENTAS")
                            .FontSize(20).Bold();
                        column.Item().AlignCenter().Text($"Período: {report.StartDate:dd/MM/yyyy} - {report.EndDate:dd/MM/yyyy}")
                            .FontSize(12);
                        column.Item().PaddingTop(5).LineHorizontal(1);
                    });

                    // Content
                    page.Content().Column(column =>
                    {
                        column.Spacing(10);

                        // Resumen General
                        column.Item().Text("RESUMEN GENERAL").FontSize(14).Bold();
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                            });

                            table.Cell().Padding(5).Text("Total de Pedidos:");
                            table.Cell().Padding(5).Text(report.TotalOrders.ToString()).Bold();

                            table.Cell().Padding(5).Text("Ingresos Totales:");
                            table.Cell().Padding(5).Text(report.TotalRevenue.ToString("C")).Bold();

                            table.Cell().Padding(5).Text("Valor Promedio:");
                            table.Cell().Padding(5).Text(report.AverageOrderValue.ToString("C")).Bold();

                            table.Cell().Padding(5).Text("Productos Vendidos:");
                            table.Cell().Padding(5).Text(report.TotalItemsSold.ToString()).Bold();
                        });

                        // Estados
                        column.Item().PaddingTop(10).Text("POR ESTADO").FontSize(14).Bold();
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Cell().Padding(5).Background(Colors.Grey.Lighten3).Text("Pendientes").Bold();
                            table.Cell().Padding(5).Background(Colors.Grey.Lighten3).Text("Procesando").Bold();
                            table.Cell().Padding(5).Background(Colors.Grey.Lighten3).Text("Enviados").Bold();
                            table.Cell().Padding(5).Background(Colors.Grey.Lighten3).Text("Entregados").Bold();
                            table.Cell().Padding(5).Background(Colors.Grey.Lighten3).Text("Cancelados").Bold();

                            table.Cell().Padding(5).AlignCenter().Text(report.PendingOrders.ToString());
                            table.Cell().Padding(5).AlignCenter().Text(report.ProcessingOrders.ToString());
                            table.Cell().Padding(5).AlignCenter().Text(report.ShippedOrders.ToString());
                            table.Cell().Padding(5).AlignCenter().Text(report.DeliveredOrders.ToString());
                            table.Cell().Padding(5).AlignCenter().Text(report.CancelledOrders.ToString());
                        });

                        // Tabla de Pedidos
                        column.Item().PaddingTop(10).Text("DETALLE DE PEDIDOS").FontSize(14).Bold();
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            // Headers
                            table.Cell().Padding(3).Background(Colors.Grey.Lighten3).Text("Número").FontSize(9).Bold();
                            table.Cell().Padding(3).Background(Colors.Grey.Lighten3).Text("Fecha").FontSize(9).Bold();
                            table.Cell().Padding(3).Background(Colors.Grey.Lighten3).Text("Cliente").FontSize(9).Bold();
                            table.Cell().Padding(3).Background(Colors.Grey.Lighten3).Text("Items").FontSize(9).Bold();
                            table.Cell().Padding(3).Background(Colors.Grey.Lighten3).Text("Total").FontSize(9).Bold();
                            table.Cell().Padding(3).Background(Colors.Grey.Lighten3).Text("Estado").FontSize(9).Bold();

                            // Data (máximo 20 para que quepa en una página)
                            foreach (var order in report.Orders.Take(20))
                            {
                                table.Cell().Padding(3).Text(order.OrderNumber).FontSize(8);
                                table.Cell().Padding(3).Text(order.OrderDate.ToString("dd/MM/yy")).FontSize(8);
                                table.Cell().Padding(3).Text(order.CustomerName).FontSize(8);
                                table.Cell().Padding(3).AlignCenter().Text(order.ItemCount.ToString()).FontSize(8);
                                table.Cell().Padding(3).Text(order.TotalAmount.ToString("C")).FontSize(8);
                                table.Cell().Padding(3).Text(order.Status.ToString()).FontSize(7);
                            }
                        });
                    });

                    // Footer
                    page.Footer().AlignRight().Text(text =>
                    {
                        text.Span("Generado el: ");
                        text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).Bold();
                    });
                });
            });

            return await Task.FromResult(document.GeneratePdf());
        }

        public async Task<byte[]> ExportProductReportToPDF(ProductReportViewModel report)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);

                    // Header
                    page.Header().Column(column =>
                    {
                        column.Item().AlignCenter().Text("REPORTE DE PRODUCTOS MÁS VENDIDOS")
                            .FontSize(20).Bold();
                        column.Item().AlignCenter().Text($"Período: {report.StartDate:dd/MM/yyyy} - {report.EndDate:dd/MM/yyyy}")
                            .FontSize(12);
                        column.Item().PaddingTop(5).LineHorizontal(1);
                    });

                    // Content
                    page.Content().Column(column =>
                    {
                        column.Spacing(10);

                        // Resumen
                        column.Item().Text("RESUMEN GENERAL").FontSize(14).Bold();
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                            });

                            table.Cell().Padding(5).Text("Total de Productos Vendidos:");
                            table.Cell().Padding(5).Text(report.TotalProductsSold.ToString()).Bold();

                            table.Cell().Padding(5).Text("Ingresos Totales:");
                            table.Cell().Padding(5).Text(report.TotalRevenue.ToString("C")).Bold();

                            table.Cell().Padding(5).Text("Productos con Stock Bajo:");
                            table.Cell().Padding(5).Text(report.LowStockProducts.ToString()).Bold();

                            table.Cell().Padding(5).Text("Productos Sin Stock:");
                            table.Cell().Padding(5).Text(report.OutOfStockProducts.ToString()).Bold();
                        });

                        // Top Productos
                        column.Item().PaddingTop(10).Text("TOP 15 PRODUCTOS").FontSize(14).Bold();
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1.5f);
                            });

                            // Headers
                            table.Cell().Padding(3).Background(Colors.Grey.Lighten3).Text("Producto").FontSize(9).Bold();
                            table.Cell().Padding(3).Background(Colors.Grey.Lighten3).Text("Categoría").FontSize(9).Bold();
                            table.Cell().Padding(3).Background(Colors.Grey.Lighten3).Text("Vendidos").FontSize(9).Bold();
                            table.Cell().Padding(3).Background(Colors.Grey.Lighten3).Text("Ingresos").FontSize(9).Bold();
                            table.Cell().Padding(3).Background(Colors.Grey.Lighten3).Text("Stock").FontSize(9).Bold();

                            // Data
                            foreach (var product in report.TopProducts.Take(15))
                            {
                                table.Cell().Padding(3).Text(product.ProductName).FontSize(8);
                                table.Cell().Padding(3).Text(product.CategoryName).FontSize(8);
                                table.Cell().Padding(3).AlignCenter().Text(product.QuantitySold.ToString()).FontSize(8);
                                table.Cell().Padding(3).Text(product.Revenue.ToString("C")).FontSize(8);
                                table.Cell().Padding(3).AlignCenter().Text(product.CurrentStock.ToString()).FontSize(8);
                            }
                        });
                    });

                    // Footer
                    page.Footer().AlignRight().Text(text =>
                    {
                        text.Span("Generado el: ");
                        text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).Bold();
                    });
                });
            });

            return await Task.FromResult(document.GeneratePdf());
        }
    }
}