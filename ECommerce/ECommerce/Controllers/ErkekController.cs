using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Controllers
{
    public class ErkekController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        public ErkekController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Giyim ürünlerini listeleme
        public IActionResult Giyim()
        {

            // Tüm kategorileri veritabanından çekip ViewBag içine ekle
            ViewBag.Categories = _dbContext.Categories.ToList();
            var products = _dbContext.Products
                .Include(p => p.Category) // Kategori bilgilerini yüklüyoruz
                .Where(p => p.Category.CategoryName == "Giyim" && p.Category.ParentCategoryID == 2) // Filtreleme
                .ToList();

            return View(products);
        }



        // Ayakkabı ürünlerini listeleme
        public IActionResult Ayakkabı()
        {
            var products = _dbContext.Products
                .Where(p => p.Category.CategoryName == "Ayakkabı" && p.Category.ParentCategoryID == 2) // Kadın > Ayakkabı
                .ToList();

            return View(products);
        }

        // Aksesuar ürünlerini listeleme
        public IActionResult Aksesuar()
        {
            var products = _dbContext.Products
                .Where(p => p.Category.CategoryName == "Aksesuar" && p.Category.ParentCategoryID == 2) // Kadın > Aksesuar
                .ToList();

            return View(products);
        }
    }
}
