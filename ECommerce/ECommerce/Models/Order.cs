using System;

namespace ECommerce.Models
{
    public class Order
    {
        public int OrderID { get; set; } // Primary Key
        public int CustomerID { get; set; } // Hangi müşteri
        public int ProductID { get; set; } // Hangi ürün
        public int Quantity { get; set; } // Adet
        public DateTime OrderDate { get; set; } = DateTime.Now; // Sipariş tarihi
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        // Navigation Properties
        public virtual Customer Customer { get; set; }
        public virtual Product Product { get; set; }
        public DateTime? ApprovalDate { get; set; } // Onay tarihi
       

        // Öncelik skoru
        public double PriorityScore { get; private set; } // Öncelik skoru

        // Öncelik skorunu güncellemek için metod
        public void UpdatePriorityScore()
        {
            if (Customer == null)
            {
                throw new InvalidOperationException("Customer bilgisi olmadan öncelik skoru hesaplanamaz.");
            }

            // Premium müşterilere daha yüksek taban skor
            double baseScore = Customer.CustomerType == "Premium" ? 15 : 10;
            double waitingTime = (DateTime.Now - OrderDate).TotalSeconds; // Geçen süre (saniye)
            double waitingTimeWeight = 0.5; // Bekleme süresine verilen ağırlık

            PriorityScore = baseScore + (waitingTime * waitingTimeWeight);
        }
    }
}
