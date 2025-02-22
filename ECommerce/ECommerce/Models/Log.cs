using Microsoft.AspNetCore.Mvc;

using System;

namespace ECommerce.Models
{
    public class Log
    {
        public int LogID { get; set; }
        public int? CustomerID { get; set; } // Müşteri işlemleri için
        public string LogType { get; set; } // "Hata", "Uyarı", "Bilgilendirme"
        public string CustomerType { get; set; } // "Premium", "Standard" veya null (admin işlemleri için)
        public string ProductName { get; set; } // Satın alınan ürün adı
        public int? Quantity { get; set; } // Satın alınan miktar
        public DateTime LogTime { get; set; } // İşlem zamanı
        public string LogMessage { get; set; } // İşlem sonucu veya hata mesajı
    }
}

