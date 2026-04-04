using DnTech_Ecommerce.Data;
using DnTech_Ecommerce.Models.Enums;
using DnTech_Ecommerce.ViewModels;
using DnTech_Ecommerce.Models;
using DnTech_Ecommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;

namespace DnTech_Ecommerce.Controllers
{    
    [Authorize(Roles = "Administrator")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly NotificationService _notificationService;

        public AdminController(ApplicationDbContext context,
                            IWebHostEnvironment environment,
                            UserManager<User> userManager,
                            RoleManager<Role> roleManager,
                            NotificationService notificationService)                            
        {
            _context = context;
            _environment = environment;
            _userManager = userManager;
            _roleManager = roleManager;
            _notificationService = notificationService;
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

                // Determinar mensaje según el nuevo estado
                string message = newStatus switch
                {
                    OrderStatus.Processing => $"Tu pedido {order.OrderNumber} está siendo procesado.",
                    OrderStatus.Shipped => $"¡Tu pedido {order.OrderNumber} ha sido enviado! Pronto llegará a tu dirección.",
                    OrderStatus.Delivered => $"¡Tu pedido {order.OrderNumber} ha sido entregado! Disfruta tu compra.",
                    OrderStatus.Cancelled => $"Tu pedido {order.OrderNumber} ha sido cancelado.",
                    _ => $"El estado de tu pedido {order.OrderNumber} ha cambiado."
                };

                // Determinar tipo de notificación
                NotificationType notificationType = newStatus switch
                {
                    OrderStatus.Shipped => NotificationType.OrderShipped,
                    OrderStatus.Delivered => NotificationType.OrderDelivered,
                    OrderStatus.Cancelled => NotificationType.OrderCancelled,
                    _ => NotificationType.OrderStatusChange
                };
            
                // Enviar notificación al cliente
                await _notificationService.CreateAndSendNotification(
                    order.UserId,
                    message,
                    notificationType,
                    $"/Orders/Details/{order.Id}",
                    order.Id
                );


                TempData["SuccessMessage"] = $"Estado del pedido actualizado a {GetStatusName(newStatus)}.";
                return RedirectToAction("OrderDetails", new { id = orderId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al actualizar el estado: {ex.Message}";
                return RedirectToAction("OrderDetails", new { id = orderId });
            }
        }

        /*
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

                // Guardar estado anterior antes de actualizar
                var oldStatus = order.Status;

                // Validar transiciones de estado (TODOS los casos)
                if (!IsValidStatusTransition(oldStatus, newStatus))
                {
                    TempData["ErrorMessage"] = $"No se puede cambiar de {oldStatus} a {newStatus}.";
                    return RedirectToAction("OrderDetails", new { id = orderId });
                }

                // Actualizar estado
                order.Status = newStatus;
                order.LastStatusUpdate = DateTime.UtcNow;
                
                // Si se entrega, registrar fecha de entrega
                if (newStatus == OrderStatus.Delivered)
                {
                    order.DeliveredDate = DateTime.UtcNow;
                }
                
                await _context.SaveChangesAsync();

                // Enviar notificación SOLO si el estado realmente cambió
                if (oldStatus != newStatus)
                {
                    await _notificationService.SendOrderStatusNotification(order, oldStatus, newStatus);
                }

                TempData["SuccessMessage"] = $"Estado del pedido actualizado de {oldStatus} a {GetStatusName(newStatus)}.";
                return RedirectToAction("OrderDetails", new { id = orderId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al actualizar el estado: {ex.Message}";
                return RedirectToAction("OrderDetails", new { id = orderId });
            }
        }

        private bool IsValidStatusTransition(OrderStatus from, OrderStatus to)
        {
            // Reglas de negocio
            return (from, to) switch
            {
                // No se puede cambiar desde estados finales
                (OrderStatus.Cancelled, _) => false,
                (OrderStatus.Delivered, _) => false,
                
                // Transiciones válidas
                (OrderStatus.Pending, OrderStatus.Processing) => true,
                (OrderStatus.Pending, OrderStatus.Cancelled) => true,
                
                (OrderStatus.Processing, OrderStatus.Shipped) => true,
                (OrderStatus.Processing, OrderStatus.Cancelled) => true,
                
                (OrderStatus.Shipped, OrderStatus.Delivered) => true,
                (OrderStatus.Shipped, OrderStatus.Cancelled) => true,
                
                // Mismo estado (no hacer nada)
                (_, _) when from == to => true,
                
                // Cualquier otra transición no permitida
                _ => false
            };
        }
        */

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

        
        // ============================================
        // GESTIÓN DE PRODUCTOS
        // ============================================

        // GET: /Admin/Products
        public async Task<IActionResult> Products(AdminProductsViewModel filter)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .AsQueryable();

            // Aplicar filtro de búsqueda
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(p =>
                    p.Name.Contains(filter.SearchTerm) ||
                    p.Description.Contains(filter.SearchTerm) ||
                    (p.Sku != null && p.Sku.Contains(filter.SearchTerm)));
            }

            // Aplicar filtro de categoría
            if (filter.FilterCategoryId.HasValue && filter.FilterCategoryId > 0)
            {
                query = query.Where(p => p.CategoryId == filter.FilterCategoryId.Value);
            }

            // Aplicar filtro de activo/inactivo
            if (filter.FilterIsActive.HasValue)
            {
                query = query.Where(p => p.IsActive == filter.FilterIsActive.Value);
            }

            // Aplicar filtro de stock bajo
            if (filter.FilterLowStock == true)
            {
                query = query.Where(p => p.StockQuantity > 0 && p.StockQuantity < 10);
            }

            // Aplicar filtro de sin stock
            if (filter.FilterOutOfStock == true)
            {
                query = query.Where(p => p.StockQuantity == 0);
            }

            // Aplicar filtro de destacados
            if (filter.FilterIsFeatured == true)
            {
                query = query.Where(p => p.IsFeatured);
            }

            // Aplicar filtro de ofertas
            if (filter.FilterIsOnSale == true)
            {
                query = query.Where(p => p.IsOnSale);
            }

            // Obtener total de productos (antes de paginación)
            var totalProducts = await query.CountAsync();

            // Calcular paginación
            var pageSize = filter.PageSize;
            var currentPage = filter.CurrentPage > 0 ? filter.CurrentPage : 1;
            var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);

            // Obtener productos paginados
            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new AdminProductSummaryViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Sku = p.Sku,
                    MainImageUrl = p.MainImageUrl,
                    Price = p.Price,
                    OldPrice = p.OldPrice,
                    StockQuantity = p.StockQuantity,
                    CategoryName = p.Category != null ? p.Category.Name : "Sin categoría",
                    IsActive = p.IsActive,
                    IsFeatured = p.IsFeatured,
                    IsOnSale = p.IsOnSale,
                    IsNew = p.IsNew,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            // Obtener estadísticas rápidas
            var allProducts = await _context.Products.ToListAsync();

            // Obtener categorías para el dropdown
            var categories = await _context.Categories
                .Where(c => c.IsActive)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToListAsync();

            var viewModel = new AdminProductsViewModel
            {
                Products = products,
                SearchTerm = filter.SearchTerm,
                FilterCategoryId = filter.FilterCategoryId,
                FilterIsActive = filter.FilterIsActive,
                FilterLowStock = filter.FilterLowStock,
                FilterOutOfStock = filter.FilterOutOfStock,
                FilterIsFeatured = filter.FilterIsFeatured,
                FilterIsOnSale = filter.FilterIsOnSale,
                CurrentPage = currentPage,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalProducts = totalProducts,
                Categories = categories,
                TotalActive = allProducts.Count(p => p.IsActive),
                TotalInactive = allProducts.Count(p => !p.IsActive),
                TotalOutOfStock = allProducts.Count(p => p.StockQuantity == 0),
                TotalLowStock = allProducts.Count(p => p.StockQuantity > 0 && p.StockQuantity < 10)
            };

            ViewData["Title"] = "Gestión de Productos";
            ViewData["Breadcrumbs"] = @"
                <li class='breadcrumb-item'><a asp-controller='Admin' asp-action='Dashboard'>Dashboard</a></li>
                <li class='breadcrumb-item active'>Productos</li>";

            return View(viewModel);
        }

        // GET: /Admin/CreateProduct
        public async Task<IActionResult> CreateProduct()
        {
            var categories = await _context.Categories
                .Where(c => c.IsActive)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToListAsync();

            var viewModel = new ProductViewModel
            {
                Categories = categories,
                IsActive = true,
                IsNew = true
            };

            ViewData["Title"] = "Crear Producto";
            ViewData["Breadcrumbs"] = @"
                <li class='breadcrumb-item'><a asp-controller='Admin' asp-action='Dashboard'>Dashboard</a></li>
                <li class='breadcrumb-item'><a asp-controller='Admin' asp-action='Products'>Productos</a></li>
                <li class='breadcrumb-item active'>Crear</li>";

            return View(viewModel);
        }

        // POST: /Admin/CreateProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(ProductViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Categories = await GetCategoriesSelectList();
                return View(model);
            }

            try
            {
                var product = new Product
                {
                    Name = model.Name,
                    Description = model.Description,
                    Price = model.Price,
                    OldPrice = model.OldPrice,
                    StockQuantity = model.StockQuantity,
                    Sku = model.Sku,
                    Slug = GenerateSlug(model.Name),
                    IsOnSale = model.IsOnSale,
                    IsFeatured = model.IsFeatured,
                    IsNew = model.IsNew,
                    IsActive = model.IsActive,
                    CategoryId = model.CategoryId,
                    CreatedAt = DateTime.Now
                };

                // Subir imagen si existe
                if (model.MainImage != null && model.MainImage.Length > 0)
                {
                    product.MainImageUrl = await SaveProductImage(model.MainImage);
                }

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Producto creado exitosamente.";
                return RedirectToAction("Products");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al crear producto: {ex.Message}";
                model.Categories = await GetCategoriesSelectList();
                return View(model);
            }
        }

        // GET: /Admin/EditProduct/5
        public async Task<IActionResult> EditProduct(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            var categories = await GetCategoriesSelectList();

            var viewModel = new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                OldPrice = product.OldPrice,
                StockQuantity = product.StockQuantity,
                Sku = product.Sku,
                MainImageUrl = product.MainImageUrl,
                IsOnSale = product.IsOnSale,
                IsFeatured = product.IsFeatured,
                IsNew = product.IsNew,
                IsActive = product.IsActive,
                CategoryId = product.CategoryId,
                Categories = categories
            };

            ViewData["Title"] = $"Editar Producto: {product.Name}";
            ViewData["Breadcrumbs"] = $@"
                <li class='breadcrumb-item'><a asp-controller='Admin' asp-action='Dashboard'>Dashboard</a></li>
                <li class='breadcrumb-item'><a asp-controller='Admin' asp-action='Products'>Productos</a></li>
                <li class='breadcrumb-item active'>Editar</li>";

            return View(viewModel);
        }

        // POST: /Admin/EditProduct/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(int id, ProductViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                model.Categories = await GetCategoriesSelectList();
                return View(model);
            }

            try
            {
                var product = await _context.Products.FindAsync(id);

                if (product == null)
                {
                    return NotFound();
                }

                product.Name = model.Name;
                product.Description = model.Description;
                product.Price = model.Price;
                product.OldPrice = model.OldPrice;
                product.StockQuantity = model.StockQuantity;
                product.Sku = model.Sku;
                product.Slug = GenerateSlug(model.Name);
                product.IsOnSale = model.IsOnSale;
                product.IsFeatured = model.IsFeatured;
                product.IsNew = model.IsNew;
                product.IsActive = model.IsActive;
                product.CategoryId = model.CategoryId;
                product.UpdatedAt = DateTime.Now;

                // Subir nueva imagen si existe
                if (model.MainImage != null && model.MainImage.Length > 0)
                {
                    // Eliminar imagen anterior si existe
                    if (!string.IsNullOrEmpty(product.MainImageUrl))
                    {
                        DeleteProductImage(product.MainImageUrl);
                    }

                    product.MainImageUrl = await SaveProductImage(model.MainImage);
                }

                _context.Update(product);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Producto actualizado exitosamente.";
                return RedirectToAction("Products");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al actualizar producto: {ex.Message}";
                model.Categories = await GetCategoriesSelectList();
                return View(model);
            }
        }

        // POST: /Admin/DeleteProduct/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);

                if (product == null)
                {
                    TempData["ErrorMessage"] = "Producto no encontrado.";
                    return RedirectToAction("Products");
                }

                // Soft delete - solo desactivar
                product.IsActive = false;
                product.UpdatedAt = DateTime.Now;

                _context.Update(product);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Producto eliminado (desactivado) exitosamente.";
                return RedirectToAction("Products");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al eliminar producto: {ex.Message}";
                return RedirectToAction("Products");
            }
        }

        // POST: /Admin/ToggleProductStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleProductStatus(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);

                if (product == null)
                {
                    TempData["ErrorMessage"] = "Producto no encontrado.";
                    return RedirectToAction("Products");
                }

                product.IsActive = !product.IsActive;
                product.UpdatedAt = DateTime.Now;

                _context.Update(product);
                await _context.SaveChangesAsync();

                var status = product.IsActive ? "activado" : "desactivado";
                TempData["SuccessMessage"] = $"Producto {status} exitosamente.";

                return RedirectToAction("Products");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cambiar estado: {ex.Message}";
                return RedirectToAction("Products");
            }
        }

        // ============================================
        // MÉTODOS AUXILIARES
        // ============================================

        private async Task<List<SelectListItem>> GetCategoriesSelectList()
        {
            return await _context.Categories
                .Where(c => c.IsActive)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToListAsync();
        }

        private string GenerateSlug(string text)
        {
            return text.ToLower()
                .Replace(" ", "-")
                .Replace("á", "a").Replace("é", "e").Replace("í", "i")
                .Replace("ó", "o").Replace("ú", "u")
                .Replace("ñ", "n");
        }

        private async Task<string> SaveProductImage(IFormFile image)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "products");
            
            // Crear directorio si no existe
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{image.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            return $"/images/products/{uniqueFileName}";
        }

        private void DeleteProductImage(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return;

            var imagePath = Path.Combine(_environment.WebRootPath, imageUrl.TrimStart('/'));
            
            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }
        }

        // ============================================
        // GESTIÓN DE USUARIOS
        // ============================================

        // GET: /Admin/Users
        public async Task<IActionResult> Users(AdminUsersViewModel filter)
        {
            var query = _context.Users.AsQueryable();

            // Aplicar filtro de búsqueda
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(u =>
                    u.FullName.Contains(filter.SearchTerm) ||
                    u.Email.Contains(filter.SearchTerm));
            }

            // Aplicar filtro de activo/inactivo
            if (filter.FilterIsActive.HasValue)
            {
                query = query.Where(u => u.Active == filter.FilterIsActive.Value);
            }

            // Obtener usuarios con sus roles
            var users = await query.OrderBy(u => u.FullName).ToListAsync();

            // Filtrar por rol si se especificó
            List<AdminUserSummaryViewModel> userSummaries = new List<AdminUserSummaryViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                
                // Aplicar filtro de rol
                if (!string.IsNullOrEmpty(filter.FilterRole) && !roles.Contains(filter.FilterRole))
                {
                    continue;
                }

                var orderCount = await _context.Orders.CountAsync(o => o.UserId == user.Id);

                userSummaries.Add(new AdminUserSummaryViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    City = user.City,
                    Active = user.Active,
                    CreatedAt = user.CreatedAt,
                    Roles = roles.ToList(),
                    TotalOrders = orderCount
                });
            }

            // Calcular paginación
            var totalUsers = userSummaries.Count;
            var pageSize = filter.PageSize;
            var currentPage = filter.CurrentPage > 0 ? filter.CurrentPage : 1;
            var totalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);

            var paginatedUsers = userSummaries
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Obtener estadísticas
            var allUsers = await _context.Users.ToListAsync();
            int totalAdmins = 0;
            int totalClients = 0;

            foreach (var user in allUsers)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                if (userRoles.Contains("Administrator"))
                    totalAdmins++;
                if (userRoles.Contains("Client"))
                    totalClients++;
            }

            var viewModel = new AdminUsersViewModel
            {
                Users = paginatedUsers,
                SearchTerm = filter.SearchTerm,
                FilterRole = filter.FilterRole,
                FilterIsActive = filter.FilterIsActive,
                CurrentPage = currentPage,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalUsers = totalUsers,
                TotalAdministrators = totalAdmins,
                TotalClients = totalClients,
                TotalActive = allUsers.Count(u => u.Active),
                TotalInactive = allUsers.Count(u => !u.Active)
            };

            ViewData["Title"] = "Gestión de Usuarios";
            ViewData["Breadcrumbs"] = @"
                <li class='breadcrumb-item'><a asp-controller='Admin' asp-action='Dashboard'>Dashboard</a></li>
                <li class='breadcrumb-item active'>Usuarios</li>";

            return View(viewModel);
        }

        // GET: /Admin/UserDetails/id
        public async Task<IActionResult> UserDetails(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            var orders = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .ToListAsync();

            var totalOrders = await _context.Orders.CountAsync(o => o.UserId == user.Id);
            var totalSpent = await _context.Orders
                .Where(o => o.UserId == user.Id && o.Status != OrderStatus.Cancelled)
                .SumAsync(o => o.Total);
            var pendingOrders = await _context.Orders
                .CountAsync(o => o.UserId == user.Id && o.Status == OrderStatus.Pending);
            var completedOrders = await _context.Orders
                .CountAsync(o => o.UserId == user.Id && o.Status == OrderStatus.Delivered);

            var viewModel = new AdminUserDetailsViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                City = user.City,
                PostalCode = user.PostalCode,
                Country = user.Country,
                Active = user.Active,
                CreatedAt = user.CreatedAt,
                CurrentRoles = roles.ToList(),
                AvailableRoles = allRoles.Select(r => new SelectListItem
                {
                    Value = r,
                    Text = r
                }).ToList(),
                TotalOrders = totalOrders,
                TotalSpent = totalSpent,
                PendingOrders = pendingOrders,
                CompletedOrders = completedOrders,
                RecentOrders = orders.Select(o => new UserOrderHistoryViewModel
                {
                    OrderId = o.Id,
                    OrderNumber = o.OrderNumber,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.Total,
                    Status = o.Status.ToString()
                }).ToList()
            };

            ViewData["Title"] = $"Usuario: {user.FullName}";
            ViewData["Breadcrumbs"] = $@"
                <li class='breadcrumb-item'><a asp-controller='Admin' asp-action='Dashboard'>Dashboard</a></li>
                <li class='breadcrumb-item'><a asp-controller='Admin' asp-action='Users'>Usuarios</a></li>
                <li class='breadcrumb-item active'>{user.FullName}</li>";

            return View(viewModel);
        }

        // POST: /Admin/ChangeUserRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeUserRole(string userId, string newRole)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado.";
                    return RedirectToAction("Users");
                }

                var currentRoles = await _userManager.GetRolesAsync(user);

                // Remover roles actuales
                if (currentRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                }

                // Asignar nuevo rol
                await _userManager.AddToRoleAsync(user, newRole);

                TempData["SuccessMessage"] = $"Rol de {user.FullName} cambiado a {newRole} exitosamente.";
                return RedirectToAction("UserDetails", new { id = userId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cambiar rol: {ex.Message}";
                return RedirectToAction("UserDetails", new { id = userId });
            }
        }

        // POST: /Admin/ToggleUserStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado.";
                    return RedirectToAction("Users");
                }

                user.Active = !user.Active;
                await _userManager.UpdateAsync(user);

                var status = user.Active ? "activado" : "desactivado";
                TempData["SuccessMessage"] = $"Usuario {status} exitosamente.";

                return RedirectToAction("UserDetails", new { id = userId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cambiar estado: {ex.Message}";
                return RedirectToAction("UserDetails", new { id = userId });
            }
        }

        // ============================================
        // GESTIÓN DE CATEGORÍAS
        // ============================================

        // GET: /Admin/Categories
        public async Task<IActionResult> Categories()
        {
            var categories = await _context.Categories
                .Include(c => c.Products)
                .OrderBy(c => c.Name)
                .ToListAsync();

            var viewModel = new AdminCategoriesViewModel
            {
                Categories = categories.Select(c => new AdminCategorySummaryViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    Slug = c.Slug,
                    IsActive = c.IsActive,
                    ProductCount = c.Products.Count,
                    CreatedAt = c.CreatedAt
                }).ToList(),
                TotalCategories = categories.Count,
                ActiveCategories = categories.Count(c => c.IsActive),
                InactiveCategories = categories.Count(c => !c.IsActive)
            };

            ViewData["Title"] = "Gestión de Categorías";
            ViewData["Breadcrumbs"] = @"
                <li class='breadcrumb-item'><a asp-controller='Admin' asp-action='Dashboard'>Dashboard</a></li>
                <li class='breadcrumb-item active'>Categorías</li>";

            return View(viewModel);
        }

        // POST: /Admin/CreateCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(AdminCategoryFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Datos inválidos. Por favor revisa el formulario.";
                return RedirectToAction("Categories");
            }

            try
            {
                var category = new Category
                {
                    Name = model.Name,
                    Description = model.Description,
                    Slug = GenerateSlug(model.Name),
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.Now
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Categoría creada exitosamente.";
                return RedirectToAction("Categories");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al crear categoría: {ex.Message}";
                return RedirectToAction("Categories");
            }
        }

        // POST: /Admin/EditCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(AdminCategoryFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Datos inválidos. Por favor revisa el formulario.";
                return RedirectToAction("Categories");
            }

            try
            {
                var category = await _context.Categories.FindAsync(model.Id);

                if (category == null)
                {
                    TempData["ErrorMessage"] = "Categoría no encontrada.";
                    return RedirectToAction("Categories");
                }

                category.Name = model.Name;
                category.Description = model.Description;
                category.Slug = GenerateSlug(model.Name);
                category.IsActive = model.IsActive;

                _context.Update(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Categoría actualizada exitosamente.";
                return RedirectToAction("Categories");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al actualizar categoría: {ex.Message}";
                return RedirectToAction("Categories");
            }
        }

        // POST: /Admin/DeleteCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                {
                    TempData["ErrorMessage"] = "Categoría no encontrada.";
                    return RedirectToAction("Categories");
                }

                // Verificar si tiene productos
                if (category.Products.Any())
                {
                    TempData["ErrorMessage"] = $"No se puede eliminar la categoría porque tiene {category.Products.Count} producto(s) asociado(s). Desactívala en su lugar.";
                    return RedirectToAction("Categories");
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Categoría eliminada exitosamente.";
                return RedirectToAction("Categories");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al eliminar categoría: {ex.Message}";
                return RedirectToAction("Categories");
            }
        }

        // POST: /Admin/ToggleCategoryStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCategoryStatus(int id)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);

                if (category == null)
                {
                    TempData["ErrorMessage"] = "Categoría no encontrada.";
                    return RedirectToAction("Categories");
                }

                category.IsActive = !category.IsActive;
                _context.Update(category);
                await _context.SaveChangesAsync();

                var status = category.IsActive ? "activada" : "desactivada";
                TempData["SuccessMessage"] = $"Categoría {status} exitosamente.";

                return RedirectToAction("Categories");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cambiar estado: {ex.Message}";
                return RedirectToAction("Categories");
            }
        }

    }
}
