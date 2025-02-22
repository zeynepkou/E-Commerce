using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Linq;
using ECommerce.Models;
using System.Diagnostics; 

namespace ECommerce.Controllers
{
    public class ProfilController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfilController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult KullanıcıProfili()
        {

            var userLoginName = HttpContext.Session.GetString("UserMail");
            Debug.WriteLine(userLoginName);

            if (string.IsNullOrEmpty(userLoginName))
            {

                return RedirectToAction("Login", "Account");
            }


            var user = _context.Customers.FirstOrDefault(c => c.CustomerLoginName == userLoginName);

            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }


            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }

            return View(user);
        }

        [HttpPost]
        public IActionResult Guncelle(Customer model)
        {
            var userLoginName = HttpContext.Session.GetString("UserMail");
            if (string.IsNullOrEmpty(userLoginName))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = _context.Customers.FirstOrDefault(c => c.CustomerLoginName == userLoginName);
            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }


            user.CustomerName = model.CustomerName;
            user.CustomerSurName = model.CustomerSurName;
            user.CustomerLoginName = model.CustomerLoginName;
            user.CustomerPassword = model.CustomerPassword;
            user.Budget = model.Budget;


            if (model.Budget < 500 || model.Budget > 3000)
            {
                TempData["Error"] = "Bütçe 500 ile 3000 arasında olmalıdır.";
                return RedirectToAction("KullanıcıProfili");
            }
            else if (model.Budget < 2000)
            {
                user.CustomerType = "Standart";
            }
            else
            {
                user.CustomerType = "Premium";
            }

            try
            {
                _context.SaveChanges();
                TempData["Message"] = "Profil başarıyla güncellendi!";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Hata: {ex.Message}");
                TempData["Error"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
            }

            return RedirectToAction("KullanıcıProfili");



        }
    }
    }
