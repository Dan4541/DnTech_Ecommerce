using DnTech_Ecommerce.Data;
using DnTech_Ecommerce.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DnTech_Ecommerce.Controllers
{
    //[ApiController]
    //[Route("[controller]/[action]")]
    //[Authorize(Roles = "Client")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: /Products
        public async Task<IActionResult> Index(ProductViewModel filter)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive);

            // Aplicar filtros
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(p =>
                    p.Name.Contains(filter.SearchTerm) ||
                    p.Description.Contains(filter.SearchTerm));
            }

            if (filter.CategoryId > 0)
            {
                query = query.Where(p => p.CategoryId == filter.CategoryId);
            }

            if (filter.MinPrice > 0)
            {
                query = query.Where(p => p.Price >= filter.MinPrice);
            }

            if (filter.MaxPrice > 0)
            {
                query = query.Where(p => p.Price <= filter.MaxPrice);
            }

            // Aplicar ordenamiento
            query = filter.SortBy switch
            {
                "price-low" => query.OrderBy(p => p.Price),
                "price-high" => query.OrderByDescending(p => p.Price),
                "name" => query.OrderBy(p => p.Name),
                "popular" => query.OrderByDescending(p => p.ReviewCount),
                _ => query.OrderByDescending(p => p.CreatedAt) // newest por defecto
            };

            var products = await query.ToListAsync();

            var viewModel = new ProductViewModel
            {
                Products = products,
                Categories = await GetCategoriesSelectList(),
                MinPrice = filter.MinPrice,
                MaxPrice = filter.MaxPrice,
                SearchTerm = filter.SearchTerm,
                SortBy = filter.SortBy
            };

            return View(viewModel);
        }

        // GET: /Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null || !product.IsActive)
            {
                return NotFound();
            }

            // Productos relacionados (misma categoría)
            var relatedProducts = await _context.Products
                .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id && p.IsActive)
                .Take(4)
                .ToListAsync();

            ViewData["RelatedProducts"] = relatedProducts;

            return View(product);
        }

        // GET: /Products/Category/{slug}
        [AllowAnonymous] // Permitir acceso sin autenticación
        public async Task<IActionResult> Category(string slug)
        {
            if (string.IsNullOrEmpty(slug))
            {
                return RedirectToAction("Index");
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Slug == slug && c.IsActive);

            if (category == null)
            {
                return NotFound();
            }

            var products = await _context.Products
                .Where(p => p.CategoryId == category.Id && p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewData["Category"] = category;
            return View(products);
        }

        // GET: /Products/Featured
        [AllowAnonymous]
        public async Task<IActionResult> Featured()
        {
            var products = await _context.Products
                .Where(p => p.IsFeatured && p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .Take(12)
                .ToListAsync();

            return View(products);
        }

        // GET: /Products/OnSale
        [AllowAnonymous]
        public async Task<IActionResult> OnSale()
        {
            var products = await _context.Products
                .Where(p => p.IsOnSale && p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .Take(12)
                .ToListAsync();

            return View(products);
        }

        // GET: /Products/New
        [AllowAnonymous]
        public async Task<IActionResult> New()
        {
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);

            var products = await _context.Products
                .Where(p => p.IsNew && p.CreatedAt >= thirtyDaysAgo && p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .Take(12)
                .ToListAsync();

            return View(products);
        }

        // Método auxiliar para obtener categorías
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
    }
}
