using DnTech_Ecommerce.Data;
using DnTech_Ecommerce.Models;
using DnTech_Ecommerce.Models.Enums;
using DnTech_Ecommerce.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DnTech_Ecommerce.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public CheckoutController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Checkout
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Obtener el carrito
            var cart = await _context.Carts
                .Include(c => c.Items)
                    .ThenInclude(ci => ci.Product)
                        .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            // Verificar que el carrito tenga items
            if (cart == null || !cart.Items.Any())
            {
                TempData["Error"] = "Tu carrito está vacío";
                return RedirectToAction("Index", "Cart");
            }

            // Verificar stock de todos los productos
            foreach (var item in cart.Items)
            {
                if (item.Product == null || !item.Product.IsActive)
                {
                    TempData["Error"] = $"El producto '{item.Product?.Name ?? "Desconocido"}' ya no está disponible";
                    return RedirectToAction("Index", "Cart");
                }

                if (item.Quantity > item.Product.StockQuantity)
                {
                    TempData["Error"] = $"No hay suficiente stock para '{item.Product.Name}'. Disponible: {item.Product.StockQuantity}";
                    return RedirectToAction("Index", "Cart");
                }
            }

            // Obtener información del usuario
            var user = await _userManager.GetUserAsync(User);

            // Preparar el ViewModel con datos del usuario
            var viewModel = new CheckoutViewModel
            {
                ShippingFullName = user?.FullName ?? "",
                ShippingEmail = user?.Email ?? "",
                ShippingAddress = user?.Address ?? "",
                ShippingCity = user?.City ?? "",
                ShippingPostalCode = user?.PostalCode ?? "",
                ShippingCountry = user?.Country ?? "Costa Rica",
                Cart = MapCartToViewModel(cart)
            };

            return View(viewModel);
        }

        // POST: /Checkout/ProcessOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessOrder(CheckoutViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Recargar el carrito para mostrar errores
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var cart = await GetCartWithItems(userId);
                model.Cart = MapCartToViewModel(cart);
                return View("Index", model);
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var cart = await GetCartWithItems(userId);

                if (cart == null || !cart.Items.Any())
                {
                    TempData["Error"] = "Tu carrito está vacío";
                    return RedirectToAction("Index", "Cart");
                }

                // Verificar stock nuevamente
                foreach (var item in cart.Items)
                {
                    if (item.Product == null || !item.Product.IsActive || item.Quantity > item.Product.StockQuantity)
                    {
                        TempData["Error"] = "Algunos productos ya no están disponibles con la cantidad solicitada";
                        return RedirectToAction("Index", "Cart");
                    }
                }

                // Crear la orden
                var order = new Order
                {
                    OrderNumber = GenerateOrderNumber(),
                    UserId = userId,

                    // Información de envío
                    ShippingFullName = model.ShippingFullName,
                    ShippingEmail = model.ShippingEmail,
                    ShippingPhone = model.ShippingPhone,
                    ShippingAddress = model.ShippingAddress,
                    ShippingCity = model.ShippingCity,
                    ShippingState = model.ShippingState,
                    ShippingPostalCode = model.ShippingPostalCode,
                    ShippingCountry = model.ShippingCountry,

                    // Montos
                    Subtotal = cart.Subtotal,
                    ShippingCost = cart.ShippingCost,
                    Tax = cart.Tax,
                    Total = cart.Total,

                    // Estado y pago
                    Status = OrderStatus.Pending,
                    PaymentMethod = model.PaymentMethod,
                    PaymentStatus = PaymentStatus.Pending,

                    Notes = model.Notes,
                    OrderDate = DateTime.Now
                };

                // Agregar items de la orden
                foreach (var cartItem in cart.Items)
                {
                    var orderItem = new OrderItem
                    {
                        ProductId = cartItem.ProductId,
                        ProductName = cartItem.Product?.Name ?? "",
                        ProductSku = cartItem.Product?.Sku,
                        Price = cartItem.Price,
                        Quantity = cartItem.Quantity
                    };

                    order.Items.Add(orderItem);

                    // Reducir el stock del producto
                    if (cartItem.Product != null)
                    {
                        cartItem.Product.StockQuantity -= cartItem.Quantity;
                    }
                }

                // Guardar la orden
                _context.Orders.Add(order);

                // Limpiar el carrito
                _context.CartItems.RemoveRange(cart.Items);

                await _context.SaveChangesAsync();

                // Redirigir a la página de confirmación
                return RedirectToAction("Confirmation", new { id = order.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al procesar el pedido: " + ex.Message);

                // Recargar el carrito
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var cart = await GetCartWithItems(userId);
                model.Cart = MapCartToViewModel(cart);

                return View("Index", model);
            }
        }

        // GET: /Checkout/Confirmation/{id}
        public async Task<IActionResult> Confirmation(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var order = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

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
                    ProductImage = oi.Product?.MainImageUrl ?? "",
                    Price = oi.Price,
                    Quantity = oi.Quantity,
                    TotalPrice = oi.TotalPrice
                }).ToList()
            };

            return View(viewModel);
        }

        // Métodos auxiliares privados
        private async Task<Cart?> GetCartWithItems(string userId)
        {
            return await _context.Carts
                .Include(c => c.Items)
                    .ThenInclude(ci => ci.Product)
                        .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }

        private CartViewModel MapCartToViewModel(Cart? cart)
        {
            if (cart == null)
            {
                return new CartViewModel
                {
                    CartId = 0,
                    Items = new List<CartItemViewModel>(),
                    Subtotal = 0,
                    ShippingCost = 0,
                    Tax = 0,
                    Total = 0,
                    TotalItems = 0
                };
            }

            var items = cart.Items.Select(ci => new CartItemViewModel
            {
                Id = ci.Id,
                ProductId = ci.ProductId,
                ProductName = ci.Product?.Name ?? "Producto no disponible",
                ProductImage = ci.Product?.MainImageUrl ?? "",
                Price = ci.Price,
                Quantity = ci.Quantity,
                MaxStock = ci.Product?.StockQuantity ?? 0,
                TotalPrice = ci.TotalPrice
            }).ToList();

            return new CartViewModel
            {
                CartId = cart.Id,
                Items = items,
                Subtotal = cart.Subtotal,
                ShippingCost = cart.ShippingCost,
                Tax = cart.Tax,
                Total = cart.Total,
                TotalItems = cart.TotalItems
            };
        }

        private string GenerateOrderNumber()
        {
            // Formato: ORD-YYYYMMDD-XXXXX
            var date = DateTime.Now.ToString("yyyyMMdd");
            var random = new Random().Next(10000, 99999);
            return $"ORD-{date}-{random}";
        }
    }
}
