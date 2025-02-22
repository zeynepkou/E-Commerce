using ECommerce.Migrations;
using ECommerce.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
namespace ECommerce.Controllers
{
    public class SepetController : Controller
    {
        private readonly ApplicationDbContext _context;

        private static readonly SemaphoreSlim _operationSemaphore = new SemaphoreSlim(1, 1); 
        private static readonly SemaphoreSlim _logSemaphore = new SemaphoreSlim(1, 1); 
        private static readonly SemaphoreSlim _consoleSemaphore = new SemaphoreSlim(1, 1); 

        private static readonly Queue<Log> _logQueue = new Queue<Log>(); 
        private static Thread _logThread; 
        private static bool _isLogging = true; 

        public SepetController(ApplicationDbContext context)
        {
            _context = context;

            if (_logThread == null)
            {
                _logThread = new Thread(ProcessLogs);
                _logThread.Start();
            }
        }


        public IActionResult Sepet()
        {
            ViewBag.Categories = _context.Categories.ToList();
            int? customerId = HttpContext.Session.GetInt32("CustomerID");
            if (customerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItems = _context.Cart
                .Include(c => c.Product)
                .Where(c => c.CustomerID == customerId)
                .ToList();

            var logs = _context.Logs
                .OrderByDescending(l => l.LogTime)
                .Take(20) 
                .ToList();

            
            QueueLog(customerId, "Bilgilendirme", "Sepet görüntülendi.");

            return View(Tuple.Create(cartItems, logs));
        }



        private void ProcessLogs()
        {
            while (_isLogging)
            {
                _logSemaphore.Wait(); 
                try
                {
                    if (_logQueue.Count > 0)
                    {
                        var log = _logQueue.Dequeue();
                        _context.Logs.Add(log);
                        _context.SaveChanges();

                        
                        _consoleSemaphore.Wait();
                        try
                        {
                            Console.WriteLine($"Log işlendi: {log.LogMessage} [{log.LogType}]");
                        }
                        finally
                        {
                            _consoleSemaphore.Release();
                        }
                    }
                }
                finally
                {
                    _logSemaphore.Release();
                }

                Thread.Sleep(100); 
            }
        }


        private void QueueLog(int? customerId, string logType, string message, string customerType = null, string productName = null, int? quantity = null)
        {
            var log = new Log
            {
                CustomerID = customerId,
                LogType = logType,
                LogMessage = message,
                CustomerType = customerType,
                ProductName = productName,
                Quantity = quantity,
                LogTime = DateTime.Now
            };

            _logSemaphore.Wait();
            try
            {
                _logQueue.Enqueue(log); 
                Console.WriteLine($"Log kuyruğa eklendi: {log.LogMessage}"); 
            }
            finally
            {
                _logSemaphore.Release();
            }
        }


        public IActionResult AddToCart(int productId)
        {
            _operationSemaphore.Wait();
            try
            {
                int? customerId = HttpContext.Session.GetInt32("CustomerID");
                if (customerId == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var product = _context.Products.Find(productId);
                if (product == null)
                {
                    QueueLog(customerId, "Hata", "Ürün bulunamadı.");
                    TempData["Message"] = "Ürün bulunamadı!";
                    return RedirectToAction("Sepet", "Sepet");
                }

                var cartItem = _context.Cart.FirstOrDefault(c => c.CustomerID == customerId && c.ProductID == productId);

                if (cartItem != null)
                {
                    if (cartItem.Quantity >= 5)
                    {
                        QueueLog(customerId, "Uyarı", "Bu üründen en fazla 5 adet alabilirsiniz.", null, product.ProductName, cartItem.Quantity);
                        TempData["Message"] = "Bu üründen en fazla 5 adet alabilirsiniz!";
                        return RedirectToAction("Sepet", "Sepet");
                    }

                    cartItem.Quantity += 1; //ürün miktarı arttırıyorum
                }
                else
                {
                    var newCartItem = new Cart
                    {
                        CustomerID = customerId.Value,
                        ProductID = productId,
                        Quantity = 1,
                        AddedDate = DateTime.Now
                    };
                    _context.Cart.Add(newCartItem);
                }

                _context.SaveChanges();

                // Loglama
                LogController.Log(
                    _context,
                    customerId,
                    "Bilgilendirme",
                    "Admin", 
                    product.ProductName, 
                    1, 
                    $"Ürün {product.ProductName} sepete eklendi.",
                    $"Ürün {product.ProductName} sepete eklendi." 
                );


                TempData["Message"] = "Ürün sepete eklendi!";
                return RedirectToAction("Sepet", "Sepet");
            }
            finally
            {
                _operationSemaphore.Release(); 
            }
        }






        protected override void Dispose(bool disposing)
        {
            _isLogging = false; 
            _logThread?.Join();
            base.Dispose(disposing);
        }


        [HttpGet]
        public IActionResult GetLogs()
        {

            var logs = _context.Logs
                .OrderByDescending(l => l.LogTime)
                .Take(50) 
                .Select(l => new
                {
                    LogTime = l.LogTime.ToString("yyyy-MM-dd HH:mm:ss"), 
                    LogType = l.LogType,
                    LogMessage = l.LogMessage
                })
                .ToList();

            return Json(logs);
        }



        public IActionResult Remove(int cartId)
        {
            SemaphoreSlim removeSemaphore = new SemaphoreSlim(1, 1);
            SemaphoreSlim logSemaphore = new SemaphoreSlim(1, 1);

            
            Thread removeThread;
            Thread logThread;

            int? customerId = HttpContext.Session.GetInt32("CustomerID");
            var customer = _context.Customers.FirstOrDefault(c => c.CustomerID == customerId);
            if (customerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItem = _context.Cart.Find(cartId);
            if (cartItem == null)
            {
                return RedirectToAction("Sepet", "Sepet");
            }

            // Sepetten ürün kaldırma işlemi
            removeThread = new Thread(() =>
            {
                removeSemaphore.Wait();
                try
                {
                    _context.Cart.Remove(cartItem);
                    _context.SaveChanges();
                    Debug.WriteLine($"[REMOVED] Cart item with ID {cartId} removed.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ERROR] Cart removal error: {ex.Message}");
                }
                finally
                {
                    removeSemaphore.Release();
                }
            });

            // Loglama işlemi
            logThread = new Thread(() =>
            {
                logSemaphore.Wait();
                try
                {
                    LogController.Log(
                        _context,
                        customerId, 
                        "Bilgilendirme", 
                        customer.CustomerType, 
                        "sepette değil", 
                        cartItem.Quantity,
                        $"Ürün {cartItem.Product?.ProductName} sepetten çıkarıldı.",
                        $"Ürün {cartItem.Product?.ProductName} sepetten çıkarıldı." 
                    );
                    Debug.WriteLine($"[LOG ADDED] Log added for cart item removal: {cartItem.Product?.ProductName}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ERROR] Log creation error: {ex.Message}");
                }
                finally
                {
                    logSemaphore.Release();
                }
            });

            
            removeThread.Start();
            logThread.Start();

            // İşlemlerin tamamlanmasını bekliyoruz
            removeThread.Join();
            logThread.Join();

            // Sepet adedini güncelleme
            UpdateCartCountInSession(customerId.Value);

            Debug.WriteLine("[COMPLETED] Cart item removal and logging completed.");
            return RedirectToAction("Sepet", "Sepet");
        }



        public IActionResult UpdateQuantity(int cartId, int quantity)
        {
            int? customerId = HttpContext.Session.GetInt32("CustomerID");
            var customer = _context.Customers.FirstOrDefault(c => c.CustomerID == customerId);
            if (customerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItem = _context.Cart.Find(cartId);
            if (cartItem != null && quantity > 0 && quantity <= 5)
            {
                cartItem.Quantity = quantity;
                _context.SaveChanges();

                // Loglama
                LogController.Log(
                    _context,
                    customerId, 
                    "Bilgilendirme", 
                    customer.CustomerType, 
                    "cartItem.Product?.ProductName, // Product Name",
                    quantity, 
                    $"Ürün {cartItem.Product?.ProductName} adedi {quantity} olarak güncellendi.",
                    $"Ürün {cartItem.Product?.ProductName} adedi {quantity} olarak güncellendi." 
                );

                
                UpdateCartCountInSession(customerId.Value);
            }
            else if (quantity > 5)
            {
                TempData["Message"] = "Bir üründen en fazla 5 adet alabilirsiniz!";
            }

            return RedirectToAction("Sepet", "Sepet");
        }


        
        private void UpdateCartCountInSession(int customerId)
        {
            var cartCount = _context.Cart.Where(c => c.CustomerID == customerId).Sum(c => c.Quantity);
            HttpContext.Session.SetInt32("CartCount", cartCount);
        }

        [HttpPost]
        public IActionResult SatisIslemi()
        {
            SemaphoreSlim cartSemaphore = new SemaphoreSlim(1, 1);
            SemaphoreSlim logSemaphore = new SemaphoreSlim(1, 1);
            SemaphoreSlim orderSemaphore = new SemaphoreSlim(1, 1);

            
            Thread cartThread;
            Thread logThread;
            Thread orderThread;

            int? customerId = HttpContext.Session.GetInt32("CustomerID");
            var customer = _context.Customers.FirstOrDefault(c => c.CustomerID == customerId);
            if (customerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItems = _context.Cart
                .Include(c => c.Product)
                .Where(c => c.CustomerID == customerId)
                .ToList();

            if (!cartItems.Any())
            {
                TempData["Message"] = "Sepetiniz boş!";

                logThread = new Thread(() =>
                {
                    logSemaphore.Wait();
                    try
                    {
                        LogController.Log(
                            _context,
                            customerId,
                            "Hata",
                            customer.CustomerType,
                            "ürün yok",
                            0,
                            "Sepet boş.",
                            "Sepet boş."
                        );
                    }
                    finally
                    {
                        logSemaphore.Release();
                    }
                });

                logThread.Start();
                logThread.Join();

                return RedirectToAction("Sepet", "Sepet");
            }

            double totalAmount = cartItems.Sum(c => c.Product.Price * c.Quantity);

            // Müşterinin bütçesini kontrol ediyorum
            
            if (customer == null || customer.Budget < totalAmount)
            {
                TempData["Message"] = "Yetersiz bütçe!";

                logThread = new Thread(() =>
                {
                    logSemaphore.Wait();
                    try
                    {
                        LogController.Log(
                            _context,
                            customerId,
                            "Hata",
                            customer.CustomerType,
                            "Bütçe yetersiz",
                            0,
                            $"Yetersiz bütçe: {totalAmount} TL",
                            "Müşterinin bütçesi yetersiz."
                        );
                    }
                    finally
                    {
                        logSemaphore.Release();
                    }
                });

                logThread.Start();
                logThread.Join();

                return RedirectToAction("Sepet", "Sepet");
            }

            var productNames = new List<string>();
            var productIds = new List<int>();

            cartThread = new Thread(() =>
            {
                cartSemaphore.Wait();
                try
                {
                    foreach (var cartItem in cartItems)
                    {
                        if (cartItem.Product.Stock < cartItem.Quantity)
                        {
                            TempData["Message"] = $"Yetersiz stok: {cartItem.Product.ProductName}";

                            logThread = new Thread(() =>
                            {
                                logSemaphore.Wait();
                                try
                                {
                                    LogController.Log(
                                        _context,
                                        customerId,
                                        "Hata",
                                        customer.CustomerType,
                                        cartItem.Product.ProductName,
                                        cartItem.Quantity,
                                        $"Yetersiz stok: {cartItem.Product?.ProductName}",
                                        $"Yetersiz stok: {cartItem.Product?.ProductName}"
                                    );
                                }
                                finally
                                {
                                    logSemaphore.Release();
                                }
                            });

                            logThread.Start();
                            logThread.Join();

                            return;
                        }

                        orderThread = new Thread(() =>
                        {
                            orderSemaphore.Wait();
                            try
                            {
                                var order = new Order
                                {
                                    CustomerID = cartItem.CustomerID,
                                    ProductID = cartItem.ProductID,
                                    Quantity = cartItem.Quantity,
                                    Status = "Pending",
                                    OrderDate = DateTime.Now
                                };

                                _context.Orders.Add(order);
                                productNames.Add(cartItem.Product.ProductName);
                                productIds.Add(cartItem.Product.ProductID);
                            }
                            finally
                            {
                                orderSemaphore.Release();
                            }
                        });

                        orderThread.Start();
                        orderThread.Join();
                    }

                    _context.Cart.RemoveRange(cartItems);
                    _context.SaveChanges();

                    // Müşteri türünü güncelle
                    double totalSpent = _context.Orders
                        .Where(o => o.CustomerID == customerId)
                        .Sum(o => o.Quantity * o.Product.Price);

                    if (totalSpent >= 2000 && customer.CustomerType != "Premium")
                    {
                        customer.CustomerType = "Premium";
                        _context.SaveChanges();
                    }
                }
                finally
                {
                    cartSemaphore.Release();
                }
            });

            cartThread.Start();
            cartThread.Join();

            logThread = new Thread(() =>
            {
                logSemaphore.Wait();
                try
                {
                    LogController.Log(
                        _context,
                        customerId,
                        "Bilgilendirme",
                        customer.CustomerType,
                        string.Join(", ", productIds),
                        cartItems.Sum(c => c.Quantity),
                        "Sipariş başarıyla alındı.",
                        "Sipariş başarıyla alındı: " + string.Join(", ", productNames)
                    );
                }
                finally
                {
                    logSemaphore.Release();
                }
            });

            logThread.Start();
            logThread.Join();

            TempData["Message"] = "Siparişiniz başarıyla alınmıştır ve admin onayı bekliyor.";
            return RedirectToAction("Sepet", "Sepet");
        }





        public IActionResult OrderSuccess()
        {
            return View();
        }
    }
}