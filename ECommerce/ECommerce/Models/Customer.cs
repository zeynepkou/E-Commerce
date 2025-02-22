namespace ECommerce.Models
{
    public class Customer
    {
        public int CustomerID { get; set; }
        public string CustomerName { get; set; }
        public string CustomerSurName { get; set; }
        public string CustomerLoginName { get; set; }
        public string CustomerPassword { get; set; }
        public double Budget { get; set; } // double olarak değiştirildi
        public string CustomerType { get; set; } // Premium / Standard
        public double TotalSpent { get; set; } // double olarak değiştirildi

        // Yeni alanlar:
        public DateTime OrderTime { get; set; } // Sipariş zamanı
        public double PriorityScore { get; private set; } // Öncelik skoru

        // Öncelik skorunu güncellemek için metod
        public void UpdatePriorityScore()
        {
            double baseScore = CustomerType == "Premium" ? 15 : 10; // Premium müşterilere daha yüksek taban skor
            double waitingTime = (DateTime.Now - OrderTime).TotalSeconds; // Geçen süre (saniye)
            double waitingTimeWeight = 0.5; // Bekleme süresine verilen ağırlık
            PriorityScore = baseScore + (waitingTime * waitingTimeWeight);
        }
    }
}
