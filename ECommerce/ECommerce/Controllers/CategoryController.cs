using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ECommerce.Controllers
{
    public class CategoryController : Controller
    {
        // Varsayılan Index Sayfası
        public IActionResult Index()
        {
            return View();
        }

        // Dinamik Yönlendirme için Genel Metod
        public IActionResult Show(string mainCategory, string subCategory)
        {
            Debug.WriteLine($"Dinamik Yönlendirme: mainCategory={mainCategory}, subCategory={subCategory}");

            // Eğer parametrelerden biri eksikse veya geçersizse ana sayfaya yönlendir
            if (string.IsNullOrEmpty(mainCategory) || string.IsNullOrEmpty(subCategory))
            {
                Debug.WriteLine("Hatalı parametreler.");
                return RedirectToAction("Index", "Home");
            }

            try
            {
                // Dinamik olarak Controller ve Action yönlendirmesi
                return RedirectToAction(subCategory, mainCategory);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Yönlendirme hatası: {ex.Message}");
                // Hatalı yönlendirme durumunda ana sayfaya yönlendir
                return RedirectToAction("Index", "Home");
            }
        }
    }
}
