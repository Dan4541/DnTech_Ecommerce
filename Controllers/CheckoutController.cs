using DnTech_Ecommerce.Data;
using DnTech_Ecommerce.Models;
using DnTech_Ecommerce.Models.Enums;
using DnTech_Ecommerce.Services;
using DnTech_Ecommerce.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DnTech_Ecommerce.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly PayPalService _payPalService;

        public CheckoutController(ApplicationDbContext context, UserManager<User> userManager, PayPalService payPalService)
        {
            _context = context;
            _userManager = userManager;
            _payPalService = payPalService;
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

                // SI ES PAYPAL, redirigir a PayPal
                if (model.PaymentMethod == PaymentMethod.PayPal)
                {
                    // Guardar datos temporales en sesión para recuperarlos después
                    HttpContext.Session.SetString("CheckoutData", System.Text.Json.JsonSerializer.Serialize(model));

                    var returnUrl = Url.Action("PayPalSuccess", "Checkout", null, Request.Scheme);
                    var cancelUrl = Url.Action("PayPalCancel", "Checkout", null, Request.Scheme);

                    // Crear orden en PayPal
                    var approvalUrl = await _payPalService.CreateOrder(cart.Total, "USD", returnUrl, cancelUrl);

                    if (string.IsNullOrEmpty(approvalUrl))
                    {
                        TempData["Error"] = "Error al conectar con PayPal. Intenta otro método de pago.";
                        model.Cart = MapCartToViewModel(cart);
                        return View("Index", model);
                    }

                    // Redirigir a PayPal
                    return Redirect(approvalUrl);
                }

                //OTROS MÉTODOS DE PAGO (tarjeta, transferencia, etc.) - Proceso normal
                var order = await CreateOrder(model, userId, cart);

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

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var cart = await GetCartWithItems(userId);
                model.Cart = MapCartToViewModel(cart);

                return View("Index", model);
            }
        }

        // ============================================
        // CALLBACKS DE PAYPAL
        // ============================================

        // GET: /Checkout/PayPalSuccess
        public async Task<IActionResult> PayPalSuccess(string token)
        {
            try
            {
                // Capturar el pago en PayPal
                var (success, transactionId, message) = await _payPalService.CaptureOrder(token);

                if (!success)
                {
                    TempData["Error"] = $"Error al procesar el pago: {message}";
                    return RedirectToAction("Index");
                }

                // Recuperar datos del checkout
                var checkoutDataJson = HttpContext.Session.GetString("CheckoutData");
                if (string.IsNullOrEmpty(checkoutDataJson))
                {
                    TempData["Error"] = "Sesión expirada. Por favor, intenta de nuevo.";
                    return RedirectToAction("Index", "Cart");
                }

                var checkoutData = System.Text.Json.JsonSerializer.Deserialize<CheckoutViewModel>(checkoutDataJson);
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var cart = await GetCartWithItems(userId);

                if (cart == null || !cart.Items.Any())
                {
                    TempData["Error"] = "Tu carrito está vacío";
                    return RedirectToAction("Index", "Cart");
                }

                // Crear la orden en nuestra BD
                var order = await CreateOrder(checkoutData, userId, cart);
                order.PaymentStatus = PaymentStatus.Completed;
                order.PaymentTransactionId = transactionId;

                _context.Orders.Add(order);
                _context.CartItems.RemoveRange(cart.Items);

                await _context.SaveChangesAsync();

                // Limpiar sesión
                HttpContext.Session.Remove("CheckoutData");

                TempData["Success"] = "¡Pago completado exitosamente!";
                return RedirectToAction("Confirmation", new { id = order.Id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al procesar el pago: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // GET: /Checkout/PayPalCancel
        public IActionResult PayPalCancel()
        {
            TempData["Warning"] = "Cancelaste el pago con PayPal. Puedes intentar de nuevo o elegir otro método de pago.";
            return RedirectToAction("Index");
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

        // ============================================
        // MÉTODOS AUXILIARES PRIVADOS
        // ============================================

        private async Task<Order> CreateOrder(CheckoutViewModel model, string userId, Cart cart)
        {
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

            return order;
        }

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
            var date = DateTime.Now.ToString("yyyyMMdd");
            var random = new Random().Next(10000, 99999);
            return $"ORD-{date}-{random}";
        }
    }
}