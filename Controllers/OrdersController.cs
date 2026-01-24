using DnTech_Ecommerce.Data;
using DnTech_Ecommerce.Models.Enums;
using DnTech_Ecommerce.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DnTech_Ecommerce.Controllers
{
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Orders
        public async Task<IActionResult> Index(string status = "all")
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Obtener todas las órdenes del usuario
            var ordersQuery = _context.Orders
                .Include(o => o.Items)
                .Where(o => o.UserId == userId);

            // Filtrar por estado si se especifica
            if (status != "all")
            {
                if (Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
                {
                    ordersQuery = ordersQuery.Where(o => o.Status == orderStatus);
                }
            }

            // Ordenar por fecha más reciente primero
            var orders = await ordersQuery
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // Mapear a ViewModel
            var viewModel = new OrderListViewModel
            {
                Orders = orders.Select(o => new OrderSummaryViewModel
                {
                    OrderId = o.Id,
                    OrderNumber = o.OrderNumber,
                    OrderDate = o.OrderDate,
                    Status = o.Status,
                    Total = o.Total,
                    TotalItems = o.TotalItems
                }).ToList(),
                TotalOrders = orders.Count
            };

            // Pasar el filtro actual a la vista
            ViewBag.CurrentFilter = status;

            return View(viewModel);
        }

        // GET: /Orders/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var order = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
            {
                TempData["Error"] = "Pedido no encontrado";
                return RedirectToAction(nameof(Index));
            }

            // Mapear a ViewModel
            var viewModel = new OrderConfirmationViewModel
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                OrderDate = order.OrderDate,
                Status = order.Status,
                PaymentMethod = order.PaymentMethod,
                PaymentStatus = order.PaymentStatus,

                ShippingFullName = order.ShippingFullName,
                ShippingEmail = order.ShippingEmail,
                ShippingPhone = order.ShippingPhone,
                ShippingAddress = order.ShippingAddress,
                ShippingCity = order.ShippingCity,
                ShippingState = order.ShippingState,
                ShippingPostalCode = order.ShippingPostalCode,
                ShippingCountry = order.ShippingCountry,

                Subtotal = order.Subtotal,
                ShippingCost = order.ShippingCost,
                Tax = order.Tax,
                Total = order.Total,
                TotalItems = order.TotalItems,

                Items = order.Items.Select(oi => new OrderItemViewModel
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    ProductName = oi.ProductName,
                    ProductSku = oi.ProductSku,
                    ProductImage = oi.Product?.MainImageUrl ?? "/images/no-image.png",
                    Price = oi.Price,
                    Quantity = oi.Quantity,
                    TotalPrice = oi.TotalPrice
                }).ToList()
            };

            return View(viewModel);
        }

        // POST: /Orders/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var order = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
            {
                return Json(new { success = false, message = "Pedido no encontrado" });
            }

            // Solo se puede cancelar si está pendiente
            if (order.Status != OrderStatus.Pending)
            {
                return Json(new
                {
                    success = false,
                    message = "Solo se pueden cancelar pedidos pendientes"
                });
            }

            try
            {
                // Cambiar estado a cancelado
                order.Status = OrderStatus.Cancelled;
                order.PaymentStatus = PaymentStatus.Refunded;

                // Devolver stock a los productos
                foreach (var item in order.Items)
                {
                    if (item.Product != null)
                    {
                        item.Product.StockQuantity += item.Quantity;
                    }
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Pedido cancelado correctamente. El stock ha sido restaurado."
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error al cancelar el pedido: " + ex.Message
                });
            }
        }
    }
}
