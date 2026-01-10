using DnTech_Ecommerce.Data;
using DnTech_Ecommerce.Models;
using DnTech_Ecommerce.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DnTech_Ecommerce.Controllers
{
    //[Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public CartController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Cart
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cart = await GetOrCreateCartAsync(userId);

            var viewModel = await MapToViewModel(cart);
            return View(viewModel);
        }

        // POST: /Cart/Add
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(AddToCartViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Datos inválidos" });
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var cart = await GetOrCreateCartAsync(userId);

                // Verificar si el producto existe y está activo
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == model.ProductId && p.IsActive);

                if (product == null)
                {
                    return Json(new { success = false, message = "Producto no encontrado" });
                }

                // Verificar stock
                if (product.StockQuantity < model.Quantity)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Solo hay {product.StockQuantity} unidades disponibles"
                    });
                }

                // Verificar si el producto ya está en el carrito
                var existingItem = await _context.CartItems
                    .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.ProductId == model.ProductId);

                if (existingItem != null)
                {
                    // Actualizar cantidad si ya existe
                    existingItem.Quantity += model.Quantity;

                    // Verificar stock total
                    if (existingItem.Quantity > product.StockQuantity)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"No hay suficiente stock. Máximo: {product.StockQuantity}"
                        });
                    }

                    existingItem.Price = product.Price; // Actualizar precio por si cambió
                }
                else
                {
                    // Crear nuevo item
                    var cartItem = new CartItem
                    {
                        CartId = cart.Id,
                        ProductId = model.ProductId,
                        Quantity = model.Quantity,
                        Price = product.Price
                    };

                    _context.CartItems.Add(cartItem);
                }

                // Actualizar fecha de modificación del carrito
                cart.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                // Obtener el nuevo conteo del carrito
                var cartItemCount = await _context.CartItems
                    .Where(ci => ci.CartId == cart.Id)
                    .SumAsync(ci => ci.Quantity);

                return Json(new
                {
                    success = true,
                    message = "Producto agregado al carrito",
                    cartItemCount = cartItemCount
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al agregar al carrito: " + ex.Message });
            }
        }

        // POST: /Cart/Update
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, int quantity)
        {
            try
            {
                var cartItem = await _context.CartItems
                    .Include(ci => ci.Product)
                    .FirstOrDefaultAsync(ci => ci.Id == id);

                if (cartItem == null)
                {
                    return Json(new { success = false, message = "Item no encontrado" });
                }

                // Verificar stock
                if (quantity > cartItem.Product?.StockQuantity)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Solo hay {cartItem.Product.StockQuantity} unidades disponibles"
                    });
                }

                cartItem.Quantity = quantity;
                cartItem.Cart.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                // Recalcular totales
                var cart = await _context.Carts
                    .Include(c => c.Items)
                        .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.Id == cartItem.CartId);

                var subtotal = cart.Items.Sum(item => item.TotalPrice);
                var shipping = subtotal > 500 ? 0 : 50;
                var tax = subtotal * 0.16m;
                var total = subtotal + shipping + tax;

                return Json(new
                {
                    success = true,
                    itemTotal = cartItem.TotalPrice.ToString("C"),
                    subtotal = subtotal.ToString("C"),
                    shipping = shipping.ToString("C"),
                    tax = tax.ToString("C"),
                    total = total.ToString("C")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al actualizar: " + ex.Message });
            }
        }

        // POST: /Cart/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            try
            {
                var cartItem = await _context.CartItems.FindAsync(id);

                if (cartItem == null)
                {
                    return Json(new { success = false, message = "Item no encontrado" });
                }

                _context.CartItems.Remove(cartItem);

                // Actualizar fecha del carrito
                var cart = await _context.Carts.FindAsync(cartItem.CartId);
                if (cart != null)
                {
                    cart.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                // Obtener nuevo conteo
                var cartItemCount = await _context.CartItems
                    .Where(ci => ci.CartId == cartItem.CartId)
                    .SumAsync(ci => ci.Quantity);

                return Json(new
                {
                    success = true,
                    message = "Producto removido del carrito",
                    cartItemCount = cartItemCount
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al remover: " + ex.Message });
            }
        }

        // POST: /Cart/Clear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var cart = await GetCartAsync(userId);

                if (cart == null)
                {
                    return Json(new { success = false, message = "Carrito no encontrado" });
                }

                // Eliminar todos los items del carrito
                var items = _context.CartItems.Where(ci => ci.CartId == cart.Id);
                _context.CartItems.RemoveRange(items);

                cart.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Carrito vaciado correctamente"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al vaciar carrito: " + ex.Message });
            }
        }

        // GET: /Cart/Count
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetCartCount()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var cart = await GetCartAsync(userId);

                if (cart == null)
                {
                    return Json(new { count = 0 });
                }

                var count = await _context.CartItems
                    .Where(ci => ci.CartId == cart.Id)
                    .SumAsync(ci => ci.Quantity);

                return Json(new { count = count });
            }
            catch
            {
                return Json(new { count = 0 });
            }
        }

        // Métodos auxiliares privados
        private async Task<Cart?> GetCartAsync(string userId)
        {
            return await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }

        private async Task<Cart> GetOrCreateCartAsync(string userId)
        {
            var cart = await GetCartAsync(userId);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }

        private async Task<CartViewModel> MapToViewModel(Cart cart)
        {
            // Cargar los items con sus productos
            await _context.Entry(cart)
                .Collection(c => c.Items)
                .Query()
                .Include(ci => ci.Product)
                    .ThenInclude(p => p.Category)
                .LoadAsync();

            var items = cart.Items.Select(ci => new CartItemViewModel
            {
                Id = ci.Id,
                ProductId = ci.ProductId,
                ProductName = ci.Product?.Name ?? "Producto no disponible",
                ProductImage = ci.Product?.MainImageUrl ?? "",
                Price = ci.Price,
                OldPrice = ci.Product?.OldPrice,
                Quantity = ci.Quantity,
                MaxStock = ci.Product?.StockQuantity ?? 0,
                TotalPrice = ci.TotalPrice,
                Slug = ci.Product?.Slug ?? "",
                CategoryName = ci.Product?.Category?.Name ?? ""
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
    }
}
