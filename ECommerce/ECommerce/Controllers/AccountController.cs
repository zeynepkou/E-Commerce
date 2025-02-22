using ECommerce.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Microsoft.AspNetCore.Http; // Session için

namespace ECommerce.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        
        [HttpGet]
        public IActionResult Login()
        {
            ViewBag.Categories = _context.Categories.ToList();
            return View("Giris");
        }


        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var customer = _context.Customers.FirstOrDefault(u => u.CustomerLoginName == email && u.CustomerPassword == password);

            if (customer != null)
            {
                
                HttpContext.Session.SetInt32("CustomerID", customer.CustomerID);
                HttpContext.Session.SetString("UserName", customer.CustomerName);
                HttpContext.Session.SetString("UserMail", customer.CustomerLoginName);

                
                var cartCount = _context.Cart
                    .Where(c => c.CustomerID == customer.CustomerID)
                    .Sum(c => c.Quantity);

                HttpContext.Session.SetInt32("CartCount", cartCount);

                TempData["Message"] = $"Hoşgeldin, {customer.CustomerName}!";
                return RedirectToAction("Index", "Home");
            }

            
            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.Error = "Müşteri giriş bilgileri hatalı!";
            return View("Giris");
        }

        
        [HttpPost]
        public IActionResult AdminLogin(string email, string password)
        {
            var admin = _context.Admins.FirstOrDefault(a => a.AdminLoginName == email && a.AdminPassword == password);

            if (admin != null)
            {
                
                HttpContext.Session.SetInt32("AdminID", admin.AdminID);
                HttpContext.Session.SetString("AdminName", admin.AdminName);

                TempData["Message"] = $"Hoşgeldin, {admin.AdminName}!";
                return RedirectToAction("Dashboard", "Admin");
            }

            
            ViewBag.Error = "Admin giriş bilgileri hatalı!";
            return View("Giris");
        }

        
        [HttpGet]
        public IActionResult Register()
        {
            ViewBag.Categories = _context.Categories.ToList();
            return View("UyeOl");
        }

        
        [HttpPost]
        public IActionResult Register(string firstName, string lastName, string email, string password, string customerType, double budget)
        {
            if (string.IsNullOrEmpty(firstName) ||
                string.IsNullOrEmpty(lastName) ||
                string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(password) ||
                string.IsNullOrEmpty(customerType) ||
                budget < 0)
            {
                ViewBag.Error = "Tüm alanları doldurmanız gerekmektedir ve bütçe negatif olamaz!";
                ViewBag.Categories = _context.Categories.ToList();
                return View("UyeOl");
            }

            var newCustomer = new Customer
            {
                CustomerName = firstName,
                CustomerSurName = lastName,
                CustomerLoginName = email,
                CustomerPassword = password,
                CustomerType = customerType,
                Budget = budget,
                TotalSpent = 0.0
            };

            _context.Customers.Add(newCustomer);
            _context.SaveChanges();

            TempData["Message"] = "Kayıt başarılı! Şimdi giriş yapabilirsiniz.";
            return RedirectToAction("Login");
        }
        public void GenerateRandomCustomers()
        {
           

            Random random = new Random();

            int customerCount = random.Next(5, 11); 
            int premiumCustomerCount = 0;

            for (int i = 0; i < customerCount; i++)
            {
                string customerType = (premiumCustomerCount < 2 || random.NextDouble() < 0.3) ? "Premium" : "Standard";

                if (customerType == "Premium")
                {
                    premiumCustomerCount++;
                }

                string firstName = "Müşteri" + random.Next(1, 1000);
                string lastName = "Soyad" + random.Next(1, 1000);
                string email = $"{firstName.ToLower()}.{lastName.ToLower()}@example.com";
                string password = "Password" + random.Next(1000, 9999);
                double budget = random.Next(500, 3001);

                var customer = new Customer
                {
                    CustomerName = firstName,
                    CustomerSurName = lastName,
                    CustomerLoginName = email,
                    CustomerPassword = password,
                    CustomerType = customerType,
                    Budget = budget,
                    TotalSpent = 0.0
                };

                _context.Customers.Add(customer);
            }

            _context.SaveChanges();
        }

       
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
