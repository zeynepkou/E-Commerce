using ECommerce.Models;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Controllers
{
    public class LogController : Controller
    {
        public static void Log(ApplicationDbContext context, int? customerId, string logType, string customerType, string productName, int? quantity, string logMessage, string detailedMessage = null)
        {
            try
            {
                var log = new Log
                {
                    CustomerID = customerId,
                    LogType = logType,
                    CustomerType = customerType,
                    ProductName = productName,
                    Quantity = quantity,
                    LogTime = DateTime.Now,
                    LogMessage = logMessage,
                };

                context.Logs.Add(log);
                context.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Log Error]: {ex.Message}");
                throw;
            }
        }


    }
}
