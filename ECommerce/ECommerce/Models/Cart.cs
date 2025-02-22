namespace ECommerce.Models
{
    public class Cart
    {
        public int CartID { get; set; } // Primary Key
        public int CustomerID { get; set; } // Hangi müşteri
        public int ProductID { get; set; } // Hangi ürün
        public int Quantity { get; set; } // Adet
        public DateTime AddedDate { get; set; } = DateTime.Now;

        // Navigation Properties
        public virtual Customer Customer { get; set; }
        public virtual Product Product { get; set; }
    }
}
