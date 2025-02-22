using ECommerce.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Linq;
using ECommerce.Hubs;
using Microsoft.AspNetCore.SignalR;
using NuGet.Common;
using ECommerce.Migrations;


namespace ECommerce.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<OrderHub> _orderHub;

        private static List<Log> LogMessages = new List<Log>(); // Log modeli tutan liste
        private static readonly SemaphoreSlim _stockUpdateSemaphore = new SemaphoreSlim(3); 
        private static readonly SemaphoreSlim _logSemaphore = new SemaphoreSlim(1); 
        private static readonly SemaphoreSlim _consoleSemaphore = new SemaphoreSlim(1, 1); 
        private static readonly Queue<Log> _logQueue = new Queue<Log>(); 
        private static Thread _logThread; 
        private static bool _isLogging = true; 

        private static readonly Dictionary<int, Mutex> _productMutexes = new();
        private static readonly Dictionary<int, DateTime> _productLockExpiry = new();
        private readonly IConfiguration _configuration;


        public AdminController(ApplicationDbContext context, IHubContext<OrderHub> orderHub, IConfiguration configuration)
        {
            _context = context;
            _orderHub = orderHub;
            _configuration = configuration; 
        }

       
        public IActionResult Dashboard()
        {
            if (HttpContext.Session.GetInt32("AdminID") == null)
                return RedirectToAction("Login", "Account");

            ViewBag.Products = _context.Products.ToList();
            return View();
        }

        
        [HttpGet]
        public async Task<IActionResult> UrunEkleme()
        {
            
            var parentCategories = await _context.Categories
                                                 .Where(c => c.ParentCategoryID == null || c.ParentCategoryID == 0)
                                                 .ToListAsync();

            
            ViewBag.Categories = parentCategories;
            var logs = _context.Logs.OrderByDescending(l => l.LogTime).Take(10).ToList(); 

            return View(logs); 

        }


        [HttpPost]
        public IActionResult UrunEkleme(Product product, IFormFile ImageUrl)
        {
            SemaphoreSlim imageUploadSemaphore = new SemaphoreSlim(1, 1);
            SemaphoreSlim productSaveSemaphore = new SemaphoreSlim(1, 1);
            SemaphoreSlim logSemaphore = new SemaphoreSlim(1, 1);

            
            Thread imageUploadThread;
            Thread productSaveThread;
            Thread logThread;

            // Resim yükleme işlemi 
            imageUploadThread = new Thread(() =>
            {
                imageUploadSemaphore.Wait();
                try
                {
                    if (ImageUrl != null && ImageUrl.Length > 0)
                    {
                        var fileName = Path.GetFileName(ImageUrl.FileName);
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/resim", fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            ImageUrl.CopyTo(stream);
                        }

                        product.ImageUrl = "/resim/" + fileName;
                        Debug.WriteLine($"[IMAGE UPLOADED] Resim Yolu: {product.ImageUrl}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ERROR] Resim yükleme hatası: {ex.Message}");
                }
                finally
                {
                    imageUploadSemaphore.Release();
                }
            });

            // Ürün kaydetme işlemi
            productSaveThread = new Thread(() =>
            {
                productSaveSemaphore.Wait();
                try
                {
                    if (ModelState.IsValid)
                    {
                        if (product.CategoryID == 0)
                        {
                            Debug.WriteLine("CategoryID eksik!");
                            TempData["ErrorMessage"] = "Lütfen bir alt kategori seçin.";
                        }
                        else
                        {
                            _context.Products.Add(product);
                            _context.SaveChanges();
                            Debug.WriteLine("[PRODUCT SAVED] Ürün Kaydedildi: KategoriID = " + product.CategoryID);
                        }
                    }
                    else
                    {
                        Debug.WriteLine("ModelState Geçersiz!");
                        foreach (var modelStateKey in ModelState.Keys)
                        {
                            var value = ModelState[modelStateKey];
                            foreach (var error in value.Errors)
                            {
                                Debug.WriteLine($"Hata: {modelStateKey} - {error.ErrorMessage}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ERROR] Ürün kaydetme hatası: {ex.Message}");
                }
                finally
                {
                    productSaveSemaphore.Release();
                }
            });

            // Log ekleme işlemi
            logThread = new Thread(() =>
            {
                logSemaphore.Wait();
                try
                {
                    var log = new Log
                    {
                        CustomerID = 1,
                        LogType = "Bilgi",
                        CustomerType = "Admin",
                        ProductName = product.ProductName,
                        Quantity = product.Stock,
                        LogMessage = $"Yeni ürün eklendi: {product.ProductName}, Kategori: {product.CategoryID}, Stok: {product.Stock}",
                        LogTime = DateTime.Now
                    };

                    lock (LogMessages)
                    {
                        LogMessages.Add(log);
                        Debug.WriteLine($"[LOG ADDED] Log added to list: {log.LogMessage}");
                    }

                    
                    using (var context = new ApplicationDbContext(
                        new DbContextOptionsBuilder<ApplicationDbContext>()
                        .UseSqlServer("Server=.;Database=ECommerce;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True")
                        .Options))
                    {
                        context.Logs.Add(log);
                        context.SaveChanges();
                        Debug.WriteLine("[LOG SAVED] Log saved to the database.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ERROR] Log ekleme hatası: {ex.Message}");
                }
                finally
                {
                    logSemaphore.Release();
                }
            });

            // Thread'leri başlatıtorum
            imageUploadThread.Start();
            productSaveThread.Start();
            logThread.Start();

            // İşlemlerin tamamlanmasını bekliyorum
            imageUploadThread.Join();
            productSaveThread.Join();
            logThread.Join();

            Debug.WriteLine("[COMPLETED] Resim yükleme, ürün kaydetme ve log ekleme işlemleri tamamlandı.");
            TempData["SuccessMessage"] = "Ürün başarıyla eklendi ve log kaydedildi!";
            return RedirectToAction("UrunEkleme");
        }





        [HttpGet]
        public IActionResult GetSubCategories(int parentId)
        {
            var subCategories = _context.Categories
                                        .Where(c => c.ParentCategoryID == parentId)
                                        .Select(c => new { c.CategoryID, c.CategoryName })
                                        .ToList();

            return Json(subCategories);
        }






        
        [HttpGet]
        public IActionResult StokGuncelleme()
        {
            var products = _context.Products.ToList();

            var logs = _context.Logs.OrderByDescending(l => l.LogTime).Take(10).ToList();
            ViewBag.Products = products;
            return View(logs);
        }


        [HttpPost]
        public IActionResult StokGuncelle(int productId, int newStock)
        {
            Mutex productMutex;

            
            lock (_productMutexes)
            {
                if (!_productMutexes.TryGetValue(productId, out productMutex))
                {
                    productMutex = new Mutex();
                    _productMutexes[productId] = productMutex;
                }
            }

            
            Thread stockUpdateThread;
            Thread logThread;

            stockUpdateThread = new Thread(() =>
              {
                  _stockUpdateSemaphore.Wait();
                  try
                  {
                      productMutex.WaitOne(); // Ürün için kilit alıyorum
                      Debug.WriteLine($"[LOCKED] Stock update started: Product ID {productId}.");

                      var product = _context.Products.Find(productId);
                      if (product != null)
                      {
                          product.Stock = newStock;
                          _context.SaveChanges();
                          Debug.WriteLine($"[UPDATED] Stock updated: Product ID {productId}, New Stock: {newStock}.");

                          // Ürünü 2 dakika boyunca kilitli tutuyoruz
                          lock (_productLockExpiry)
                          {
                              _productLockExpiry[productId] = DateTime.Now.AddMinutes(2);
                          }
                      }
                      else
                      {
                          Debug.WriteLine($"[NOT FOUND] Product not found: Product ID {productId}.");
                      }
                  }
                  catch (Exception ex)
                  {
                      Debug.WriteLine($"[ERROR] Error during stock update: {ex.Message}.");
                  }
                  finally
                  {
                      productMutex.ReleaseMutex(); // Kilidi serbest bıraktık
                      Debug.WriteLine($"[RELEASED] Stock update finished: Product ID {productId}.");
                      _stockUpdateSemaphore.Release();
                  }
              });

            // Log kaydetme işlemi
            logThread = new Thread(() =>
            {
                _logSemaphore.Wait();
                try
                {
                    var log = new Log
                    {
                        CustomerID = 1,
                        LogType = "Bilgi",
                        CustomerType = "Admin",
                        ProductName = productId.ToString(),
                        Quantity = 0,
                        LogMessage = $"Stok başarıyla güncellendi: Product ID {productId}, Yeni Stok: {newStock}",
                        LogTime = DateTime.Now
                    };

                    lock (LogMessages)
                    {
                        LogMessages.Add(log);
                        Debug.WriteLine($"[LOG ADDED] Log added to list: {log.LogMessage}");
                    }

                   
                    using (var context = new ApplicationDbContext(
                        new DbContextOptionsBuilder<ApplicationDbContext>()
                        .UseSqlServer("Server=.;Database=ECommerce;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True")
                        .Options))
                    {
                        context.Logs.Add(log);
                        context.SaveChanges();
                        Debug.WriteLine("[SAVED] Log saved to the database.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ERROR] Error while processing log: {ex.Message}");
                }
                finally
                {
                    _logSemaphore.Release();
                }
            });

            
            stockUpdateThread.Start();
            logThread.Start();

            
            stockUpdateThread.Join();
            logThread.Join();

            Debug.WriteLine("[COMPLETED] Stock update and logging finished.");
            return RedirectToAction("StokGuncelleme");
        }

        // Diğer metotlarda kilit kontrolü yaptığımız metod
        private bool IsProductLocked(int productId)
        {
            lock (_productLockExpiry)
            {
                return _productLockExpiry.TryGetValue(productId, out var expiryTime) && expiryTime > DateTime.Now;
            }
        }


        [HttpGet]
        public IActionResult GetLogs()
        {
            var logs = _context.Logs
                .OrderByDescending(l => l.LogTime)
                .Take(15) 
                .Select(l => new
                {
                    LogTime = l.LogTime.ToString("yyyy-MM-dd HH:mm:ss"), 
                    LogType = l.LogType,
                    LogMessage = l.LogMessage
                })
                .ToList();

            return Json(logs);
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


        [HttpGet]
        public IActionResult UrunSilme()
        {
            var products = _context.Products.ToList();

            if (products == null || !products.Any())
            {
                ViewBag.Products = new List<Product>();
            }
            else
            {
                ViewBag.Products = products;
            }
            var logs = _context.Logs.OrderByDescending(l => l.LogTime).Take(10).ToList(); 

            return View(logs);  

        }

        [HttpPost]
        public IActionResult UrunSilme(int productId)
        {
            SemaphoreSlim deleteSemaphore = new SemaphoreSlim(1, 1);
            SemaphoreSlim logSemaphore = new SemaphoreSlim(1, 1);

            // Ürün kilitli mi kontrol ediyoruz
            if (IsProductLocked(productId))
            {
                Debug.WriteLine($"[DENIED] Product ID {productId} is locked and cannot be deleted.");
                return Json(new { success = false, message = "Bu ürün şu anda kilitli. Lütfen daha sonra tekrar deneyin." });
            }

            
            Thread deleteThread;
            Thread logThread;

            var product = _context.Products.Find(productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Ürün bulunamadı." });
            }

            // Ürün silme işlemi
            deleteThread = new Thread(() =>
            {
                deleteSemaphore.Wait();
                try
                {
                    _context.Products.Remove(product);
                    _context.SaveChanges();
                    Debug.WriteLine($"[DELETED] Ürün silindi: Product ID {productId}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ERROR] Ürün silme hatası: {ex.Message}");
                }
                finally
                {
                    deleteSemaphore.Release();
                }
            });

            // Log ekleme işlemi
            logThread = new Thread(() =>
            {
                logSemaphore.Wait();
                try
                {
                    var log = new Log
                    {
                        CustomerID = 1,
                        LogType = "Bilgi",
                        CustomerType = "Admin",
                        ProductName = product.ProductName,
                        Quantity = product.Stock,
                        LogMessage = $"Ürün silindi: {product.ProductName}, ID: {productId}",
                        LogTime = DateTime.Now
                    };

                    lock (LogMessages)
                    {
                        LogMessages.Add(log);
                        Debug.WriteLine($"[LOG ADDED] Log added to list: {log.LogMessage}");
                    }

                    
                    using (var context = new ApplicationDbContext(
                        new DbContextOptionsBuilder<ApplicationDbContext>()
                        .UseSqlServer("Server=.;Database=ECommerce;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True")
                        .Options))
                    {
                        context.Logs.Add(log);
                        context.SaveChanges();
                        Debug.WriteLine("[LOG SAVED] Log saved to the database.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ERROR] Log ekleme hatası: {ex.Message}");
                }
                finally
                {
                    logSemaphore.Release();
                }
            });

            
            deleteThread.Start();
            logThread.Start();

            
            deleteThread.Join();
            logThread.Join();

            Debug.WriteLine("[COMPLETED] Ürün silme ve log ekleme işlemleri tamamlandı.");
            return Json(new { success = true, message = "Ürün başarıyla silindi." });
        }



        public IActionResult Siparis()
        {
            var pendingOrders = _context.Orders
    .Include(o => o.Customer) 
    .Include(o => o.Product)  
    .Where(o => o.Status == "Pending") 
    .ToList();

            
            foreach (var order in pendingOrders)
            {
                order.UpdatePriorityScore();
                Debug.WriteLine(order.PriorityScore);
            }


            // Öncelik skoruna göre sıralıyoruz
            pendingOrders = pendingOrders.OrderByDescending(o => o.PriorityScore).ToList();
            var logs = _context.Logs.OrderByDescending(l => l.LogTime).Take(10).ToList();
            ViewBag.Logs = logs;
            return View(pendingOrders);
        }


        [HttpPost]
        public IActionResult Onayla(int orderId)
        {
            var order = _context.Orders.Include(o => o.Product).FirstOrDefault(o => o.OrderID == orderId);

            if (order == null)
            {
                return Json(new { success = false, message = "Sipariş bulunamadı." });
            }

            if (order.Product.Stock < order.Quantity)
            {
                return Json(new { success = false, message = "Yetersiz stok!" });
            }

            
            order.Product.Stock -= order.Quantity;
            order.Status = "Approved";
            _context.SaveChanges();

            return Json(new { success = true, message = "Sipariş onaylandı." });
        }

        [HttpPost]
        public IActionResult Reddet(int orderId)
        {
            var order = _context.Orders.Find(orderId);

            if (order == null)
            {
                return Json(new { success = false, message = "Sipariş bulunamadı." });
            }

            // Sipariş durumunu güncelle
            order.Status = "Rejected";
            _context.SaveChanges();

            return Json(new { success = true, message = "Sipariş reddedildi." });
        }





        private static readonly Mutex _mutex = new Mutex(); // Kritik bölgeyi koruyan mutex tanımladık

        [HttpPost]
        public IActionResult OnaylaSiparisleri()
        {
            // Bekleyen siparişleri al ve öncelik skoruna göre sırala
            var pendingOrders = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Product)
                .Where(o => o.Status == "Pending")
                .OrderByDescending(o => o.PriorityScore)
                .ToList();

            // Öncelik skorlarını sabitle
            foreach (var order in pendingOrders)
            {
                order.UpdatePriorityScore();
            }
            _context.SaveChanges();

            
            Debug.WriteLine("=== Siparişlerin Öncelik Sırasına Göre İşlenme Listesi ===");
            foreach (var order in pendingOrders)
            {
                Debug.WriteLine($"Sipariş ID: {order.OrderID}, Müşteri: {order.Customer.CustomerName} {order.Customer.CustomerSurName}, Ürün: {order.Product.ProductName}, Öncelik Skoru: {order.PriorityScore:F2}");
            }

            // Thread havuzu oluşturma
            int maxThreads = 4; 
            var threads = new List<Thread>();
            var processingOrders = new Queue<Order>(pendingOrders);

            int activeThreads = 0; 
            object lockObj = new object(); 

            while (processingOrders.Count > 0 || activeThreads > 0)
            {
                lock (lockObj)
                {
                    // Eğer aktif thread sayısı maksimumdan azsa ve işlenecek sipariş varsa
                    if (activeThreads < maxThreads && processingOrders.Count > 0)
                    {
                        var order = processingOrders.Dequeue();

                        var thread = new Thread(() =>
                        {
                            try
                            {
                                // Aktif thread sayısını artır
                                Interlocked.Increment(ref activeThreads);
                                Debug.WriteLine($"[THREAD STARTED] Aktif thread sayısı: {activeThreads}");

                                // Siparişi işlemesi için processordera gönderiyoruz
                                ProcessOrder(order);

                                Debug.WriteLine($"[PROCESS COMPLETED] Sipariş işlendi: Sipariş ID: {order.OrderID}");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[ERROR] Error while processing order ID {order.OrderID}: {ex.Message}");
                            }
                            finally
                            {
                                // Aktif thread sayısını azalt
                                Interlocked.Decrement(ref activeThreads);
                                Debug.WriteLine($"[THREAD FINISHED] Aktif thread sayısı: {activeThreads}");
                            }
                        });

                        threads.Add(thread);
                        thread.Start();
                    }
                }

                
                Thread.Sleep(50);
            }

            
            foreach (var thread in threads)
            {
                thread.Join();
            }

            TempData["Message"] = "Tüm siparişler başarıyla işlendi.";
            return RedirectToAction("Siparis");
        }

        private void ProcessOrder(Order order)
        {
            Mutex productMutex;

          
            lock (_productMutexes)
            {
                if (!_productMutexes.TryGetValue(order.Product.ProductID, out productMutex))
                {
                    productMutex = new Mutex();
                    _productMutexes[order.Product.ProductID] = productMutex;
                }
            }

            // İşleme threadi
            productMutex.WaitOne();
            try
            {
                
                SiparisDurumu(order.OrderID, "Processing");
                Debug.WriteLine($"[PROCESSING START] Sipariş işleniyor: Sipariş ID: {order.OrderID}, Müşteri: {order.Customer.CustomerName} {order.Customer.CustomerSurName}, Ürün: {order.Product.ProductName}, Öncelik Skoru: {order.PriorityScore:F2}");
                var logThread = new Thread(() => LogOrderStatus(order, "Sipariş işleniyor"));
                logThread.Start();
                
                Thread.Sleep(5000); 

                
                var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                optionsBuilder.UseSqlServer(_configuration.GetConnectionString("DefaultConnection"));

                using (var context = new ApplicationDbContext(optionsBuilder.Options))
                {
                    
                    var dbOrder = context.Orders
                        .Include(o => o.Product)
                        .Include(o => o.Customer)
                        .FirstOrDefault(o => o.OrderID == order.OrderID);

                    if (dbOrder != null && dbOrder.Product.Stock >= dbOrder.Quantity)
                    {
                        double totalPrice = dbOrder.Quantity * dbOrder.Product.Price;

                        
                        var customer = context.Customers.FirstOrDefault(c => c.CustomerID == dbOrder.CustomerID);

                        if (customer != null && customer.Budget >= totalPrice)
                        {
                            
                            double oldBudget = customer.Budget;

                            
                            customer.Budget -= totalPrice;
                            customer.TotalSpent = (customer.TotalSpent) + totalPrice;
                            context.SaveChanges(); 
                            dbOrder.Product.Stock -= dbOrder.Quantity;
                            
                            dbOrder.Status = "Approved";

                            
                            Debug.WriteLine($"[APPROVED] Sipariş onaylandı: Sipariş ID: {dbOrder.OrderID}");
                            Debug.WriteLine($"Müşteri: {dbOrder.Customer.CustomerName} {dbOrder.Customer.CustomerSurName}");
                            Debug.WriteLine($"Ürün: {dbOrder.Product.ProductName}, Miktar: {dbOrder.Quantity}, Birim Fiyat: {dbOrder.Product.Price:F2}");
                            Debug.WriteLine($"Toplam Fiyat: {totalPrice:F2}");
                            Debug.WriteLine($"Müşteri Eski Bütçe: {oldBudget:F2}, Yeni Bütçe: {dbOrder.Customer.Budget:F2}");
                            Debug.WriteLine($"Stok Kalan: {dbOrder.Product.Stock}");

                            SiparisDurumu(dbOrder.OrderID, "Approved");

                            // Log işlemi
                            logThread = new Thread(() => LogOrderStatus(dbOrder, "Sipariş onaylandı"));
                            logThread.Start();
                        }
                        else
                        {
                            // Bütçe yetersizse
                            Debug.WriteLine($"[FAILED] Sipariş reddedildi: Sipariş ID: {dbOrder.OrderID}, Müşteri bütçesi yetersiz.");
                            Debug.WriteLine($"Gerekli Bütçe: {totalPrice:F2}, Mevcut Bütçe: {dbOrder.Customer.Budget:F2}");
                            dbOrder.Status = "Failed";
                            SiparisDurumu(dbOrder.OrderID, "Failed");
                        }
                    }
                   
                    else
                    {
                        // Stok yetersizse
                        if (dbOrder != null)
                        {
                            dbOrder.Status = "Rejected";
                            Debug.WriteLine($"[REJECTED] Sipariş reddedildi: Sipariş ID: {dbOrder.OrderID}, Müşteri: {dbOrder.Customer.CustomerName} {dbOrder.Customer.CustomerSurName}, Ürün: {dbOrder.Product.ProductName}, Öncelik Skoru: {dbOrder.PriorityScore:F2}");
                            SiparisDurumu(dbOrder.OrderID, "Rejected");

                            
                            logThread = new Thread(() => LogOrderStatus(dbOrder, "Sipariş reddedildi"));
                            logThread.Start();
                        }
                    }

                    
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Sipariş işlenirken hata oluştu: {ex.Message}, Sipariş ID: {order.OrderID}");
                SiparisDurumu(order.OrderID, "Error");

                
                var logThread = new Thread(() => LogOrderStatus(order, "Sipariş işlenirken hata oluştu"));
                logThread.Start();
            }
            finally
            {
                productMutex.ReleaseMutex();
            }
        }


        private void LogOrderStatus(Order order, string message)
        {
            var log = new Log
            {
                CustomerID = order.Customer.CustomerID,
                LogType = "Sipariş",
                CustomerType = order.Customer.CustomerType,
                ProductName = order.Product.ProductName,
                Quantity = order.Quantity,
                LogMessage = $"{message}: Sipariş ID {order.OrderID}, Durum: {order.Status}",
                LogTime = DateTime.Now
            };

            try
            {
                lock (LogMessages)
                {
                    LogMessages.Add(log);
                    Debug.WriteLine($"[LOG ADDED] Log added for order ID {order.OrderID}: {log.LogMessage}");
                }

                using (var context = new ApplicationDbContext(
                    new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseSqlServer(_configuration.GetConnectionString("DefaultConnection"))
                    .Options))
                {
                    context.Logs.Add(log);
                    context.SaveChanges();
                    Debug.WriteLine("[SAVED] Log saved to the database.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Error while logging order ID {order.OrderID}: {ex.Message}");
            }
        }


        private void SiparisDurumu(int orderId, string status)
        {
            try
            {
                _orderHub.Clients.All.SendAsync("OrderStatus", orderId, status).Wait();
            }
            catch (Exception ex)
            {
               // LogMessage($"SignalR bildirimi başarısız: Sipariş ID: {orderId}, Hata: {ex.Message}");
            }
        }

       


       

       
        [HttpGet]
        public IActionResult StartPriorityScoreUpdate()
        {
            Task.Run(async () =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                optionsBuilder.UseSqlServer(_configuration.GetConnectionString("DefaultConnection"));

                while (true)
                {
                    using (var context = new ApplicationDbContext(optionsBuilder.Options))
                    {
                        var pendingOrders = await context.Orders
                            .Include(o => o.Customer)
                            .Include(o => o.Product)
                            .Where(o => o.Status == "Pending")
                            .ToListAsync();

                        if (pendingOrders.Any())
                        {
                            foreach (var order in pendingOrders)
                            {
                                order.UpdatePriorityScore();
                            }

                            await context.SaveChangesAsync();

                            var updatedOrders = pendingOrders
                                .OrderByDescending(o => o.PriorityScore)
                                .Select(o => new
                                {
                                    o.OrderID,
                                    CustomerName = $"{o.Customer.CustomerName} {o.Customer.CustomerSurName}",
                                    o.Product.ProductName,
                                    o.Quantity,
                                    o.Status,
                                    o.PriorityScore
                                }).ToList();

                            Debug.WriteLine("Updated Orders Sent: " + updatedOrders.Count);
                            await _orderHub.Clients.All.SendAsync("UpdateOrderList", updatedOrders);
                        }
                    }

                    Thread.Sleep(1000);

                }
            });

            return Json(new { success = true });
        } 







    }

}

