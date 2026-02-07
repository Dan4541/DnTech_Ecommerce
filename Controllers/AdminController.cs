using DnTech_Ecommerce.Data;
using DnTech_Ecommerce.Models.Enums;
using DnTech_Ecommerce.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DnTech_Ecommerce.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfLastMonth = startOfMonth.AddMonths(-1);
            var endOfLastMonth = startOfMonth.AddDays(-1);

            var viewModel = new DashboardViewModel();

            // ============================================
            // MÉTRICAS DE PEDIDOS
            // ============================================

            viewModel.TotalOrdersToday = await _context.Orders
                .Where(o => o.OrderDate.Date == today)
                .CountAsync();

            viewModel.TotalOrdersWeek = await _context.Orders
                .Where(o => o.OrderDate >= startOfWeek)
                .CountAsync();

            viewModel.TotalOrdersMonth = await _context.Orders
                .Where(o => o.OrderDate >= startOfMonth)
                .CountAsync();

            viewModel.PendingOrders = await _context.Orders
                .Where(o => o.Status == OrderStatus.Pending)
                .CountAsync();

            // Pedidos del mes anterior (para comparación)
            var lastMonthOrders = await _context.Orders
                .Where(o => o.OrderDate >= startOfLastMonth && o.OrderDate <= endOfLastMonth)
                .CountAsync();

            // Calcular cambio porcentual de pedidos
            if (lastMonthOrders > 0)
            {
                viewModel.OrdersChangePercent = ((decimal)(viewModel.TotalOrdersMonth - lastMonthOrders) / lastMonthOrders) * 100;
            }

            // ============================================
            // MÉTRICAS DE INGRESOS
            // ============================================

            viewModel.RevenueToday = await _context.Orders
                .Where(o => o.OrderDate.Date == today && o.Status != OrderStatus.Cancelled)
                .SumAsync(o => o.Total);

            viewModel.RevenueWeek = await _context.Orders
                .Where(o => o.OrderDate >= startOfWeek && o.Status != OrderStatus.Cancelled)
                .SumAsync(o => o.Total);

            viewModel.RevenueMonth = await _context.Orders
                .Where(o => o.OrderDate >= startOfMonth && o.Status != OrderStatus.Cancelled)
                .SumAsync(o => o.Total);

            // Ingresos del mes anterior (para comparación)
            var lastMonthRevenue = await _context.Orders
                .Where(o => o.OrderDate >= startOfLastMonth && o.OrderDate <= endOfLastMonth && o.Status != OrderStatus.Cancelled)
                .SumAsync(o => o.Total);

            // Calcular cambio porcentual de ingresos
            if (lastMonthRevenue > 0)
            {
                viewModel.RevenueChangePercent = ((viewModel.RevenueMonth - lastMonthRevenue) / lastMonthRevenue) * 100;
            }

            // ============================================
            // MÉTRICAS DE PRODUCTOS
            // ============================================

            viewModel.TotalProducts = await _context.Products.CountAsync();
            viewModel.ActiveProducts = await _context.Products.Where(p => p.IsActive).CountAsync();
            viewModel.OutOfStockProducts = await _context.Products.Where(p => p.StockQuantity == 0).CountAsync();
            viewModel.LowStockProducts = await _context.Products.Where(p => p.StockQuantity > 0 && p.StockQuantity < 10).CountAsync();

            // ============================================
            // MÉTRICAS DE USUARIOS
            // ============================================

            viewModel.TotalUsers = await _context.Users.CountAsync();
            viewModel.NewUsersThisMonth = await _context.Users
                .Where(u => u.CreatedAt >= startOfMonth)
                .CountAsync();

            // ============================================
            // PEDIDOS RECIENTES (últimos 10)
            // ============================================

            viewModel.RecentOrders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Items)
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .Select(o => new RecentOrderViewModel
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    CustomerName = o.User.FullName,
                    CustomerEmail = o.User.Email,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.Total,
                    Status = o.Status,
                    ItemCount = o.Items.Count
                })
                .ToListAsync();

            // ============================================
            // PRODUCTOS MÁS VENDIDOS (Top 5)
            // ============================================

            viewModel.TopProducts = await _context.OrderItems
                .Include(oi => oi.Product)
                .Where(oi => oi.Order.Status != OrderStatus.Cancelled)
                .GroupBy(oi => new
                {
                    oi.ProductId,
                    oi.Product.Name,
                    oi.Product.MainImageUrl,
                    oi.Product.StockQuantity
                })
                .Select(g => new TopProductViewModel
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.Name,
                    ImageUrl = g.Key.MainImageUrl,
                    CurrentStock = g.Key.StockQuantity,
                    TotalSold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.Price * oi.Quantity)
                })
                .OrderByDescending(p => p.TotalSold)
                .Take(5)
                .ToListAsync();

            ViewData["Title"] = "Dashboard";
            ViewData["Breadcrumbs"] = "<li class='breadcrumb-item active'>Dashboard</li>";

            return View(viewModel);
        }

        // ============================================
        // GESTIÓN DE PEDIDOS
        // ============================================

        // GET: /Admin/Orders
        public async Task<IActionResult> Orders(AdminOrdersViewModel filter)
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.Items)
                .AsQueryable();

            // Aplicar filtro de búsqueda (número de orden o email)
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(o =>
                    o.OrderNumber.Contains(filter.SearchTerm) ||
                    o.User.Email.Contains(filter.SearchTerm) ||
                    o.User.FullName.Contains(filter.SearchTerm));
            }

            // Aplicar filtro de estado
            if (filter.FilterStatus.HasValue)
            {
                query = query.Where(o => o.Status == filter.FilterStatus.Value);
            }

            // Aplicar filtro de fecha desde
            if (filter.FilterDateFrom.HasValue)
            {
                query = query.Where(o => o.OrderDate.Date >= filter.FilterDateFrom.Value.Date);
            }

            // Aplicar filtro de fecha hasta
            if (filter.FilterDateTo.HasValue)
            {
                query = query.Where(o => o.OrderDate.Date <= filter.FilterDateTo.Value.Date);
            }

            // Aplicar filtro de monto mínimo
            if (filter.FilterMinAmount.HasValue)
            {
                query = query.Where(o => o.Total >= filter.FilterMinAmount.Value);
            }

            // Aplicar filtro de monto máximo
            if (filter.FilterMaxAmount.HasValue)
            {
                query = query.Where(o => o.Total <= filter.FilterMaxAmount.Value);
            }

            // Obtener total de pedidos (antes de paginación)
            var totalOrders = await query.CountAsync();

            // Calcular paginación
            var pageSize = filter.PageSize;
            var currentPage = filter.CurrentPage > 0 ? filter.CurrentPage : 1;
            var totalPages = (int)Math.Ceiling(totalOrders / (double)pageSize);

            // Obtener pedidos paginados
            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new AdminOrderSummaryViewModel
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    CustomerName = o.User.FullName,
                    CustomerEmail = o.User.Email,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.Total,
                    Status = o.Status,
                    PaymentMethod = o.PaymentMethod,
                    ItemCount = o.Items.Count
                })
                .ToListAsync();

            // Obtener estadísticas rápidas
            var allOrders = await _context.Orders.ToListAsync();

            var viewModel = new AdminOrdersViewModel
            {
                Orders = orders,
                SearchTerm = filter.SearchTerm,
                FilterStatus = filter.FilterStatus,
                FilterDateFrom = filter.FilterDateFrom,
                FilterDateTo = filter.FilterDateTo,
                FilterMinAmount = filter.FilterMinAmount,
                FilterMaxAmount = filter.FilterMaxAmount,
                CurrentPage = currentPage,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalOrders = totalOrders,
                TotalPending = allOrders.Count(o => o.Status == OrderStatus.Pending),
                TotalProcessing = allOrders.Count(o => o.Status == OrderStatus.Processing),
                TotalShipped = allOrders.Count(o => o.Status == OrderStatus.Shipped),
                TotalDelivered = allOrders.Count(o => o.Status == OrderStatus.Delivered),
                TotalCancelled = allOrders.Count(o => o.Status == OrderStatus.Cancelled)
            };

            ViewData["Title"] = "Gestión de Pedidos";
            ViewData["Breadcrumbs"] = @"
        <li class='breadcrumb-item'><a asp-controller='Admin' asp-action='Dashboard'>Dashboard</a></li>
        <li class='breadcrumb-item active'>Pedidos</li>";

            return View(viewModel);
        }

        // GET: /Admin/OrderDetails/5
        public async Task<IActionResult> OrderDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            var viewModel = new AdminOrderDetailsViewModel
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                OrderDate = order.OrderDate,
                Status = order.Status,
                PaymentMethod = order.PaymentMethod,
                PaymentStatus = order.PaymentStatus,
                CustomerId = order.UserId,
                CustomerName = order.User.FullName,
                CustomerEmail = order.User.Email,
                CustomerPhone = order.User.PhoneNumber ?? "N/A",
                ShippingAddress = order.ShippingAddress,
                ShippingCity = order.ShippingCity,
                ShippingPostalCode = order.ShippingPostalCode,
                ShippingCountry = order.ShippingCountry,
                SubTotal = order.Subtotal,
                ShippingCost = order.ShippingCost,
                Tax = order.Tax,
                TotalAmount = order.Total,
                Notes = order.Notes,
                //CancelledAt = order.CancelledAt,
                //CancellationReason = order.CancellationReason,
                Items = order.Items.Select(oi => new AdminOrderItemViewModel
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product.Name,
                    ProductImageUrl = oi.Product.MainImageUrl,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.Price
                }).ToList()
            };

            ViewData["Title"] = $"Pedido {order.OrderNumber}";
            ViewData["Breadcrumbs"] = $@"
        <li class='breadcrumb-item'><a asp-controller='Admin' asp-action='Dashboard'>Dashboard</a></li>
        <li class='breadcrumb-item'><a asp-controller='Admin' asp-action='Orders'>Pedidos</a></li>
        <li class='breadcrumb-item active'>{order.OrderNumber}</li>";

            return View(viewModel);
        }

        // POST: /Admin/UpdateOrderStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, OrderStatus newStatus)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Pedido no encontrado.";
                    return RedirectToAction("Orders");
                }

                // Validar transición de estado
                if (order.Status == OrderStatus.Cancelled)
                {
                    TempData["ErrorMessage"] = "No se puede cambiar el estado de un pedido cancelado.";
                    return RedirectToAction("OrderDetails", new { id = orderId });
                }

                if (order.Status == OrderStatus.Delivered && newStatus != OrderStatus.Delivered)
                {
                    TempData["ErrorMessage"] = "No se puede cambiar el estado de un pedido ya entregado.";
                    return RedirectToAction("OrderDetails", new { id = orderId });
                }

                order.Status = newStatus;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Estado del pedido actualizado a {GetStatusName(newStatus)}.";
                return RedirectToAction("OrderDetails", new { id = orderId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al actualizar el estado: {ex.Message}";
                return RedirectToAction("OrderDetails", new { id = orderId });
            }
        }

        // Método auxiliar para obtener nombre del estado en español
        private string GetStatusName(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => "Pendiente",
                OrderStatus.Processing => "Procesando",
                OrderStatus.Shipped => "Enviado",
                OrderStatus.Delivered => "Entregado",
                OrderStatus.Cancelled => "Cancelado",
                _ => status.ToString()
            };
        }

        // Placeholder para las demás acciones (las implementaremos después)
        public IActionResult Products()
        {
            ViewData["Title"] = "Gestión de Productos";
            return View();
        }

        public IActionResult Users()
        {
            ViewData["Title"] = "Gestión de Usuarios";
            return View();
        }

        public IActionResult Categories()
        {
            ViewData["Title"] = "Gestión de Categorías";
            return View();
        }
    }
}
