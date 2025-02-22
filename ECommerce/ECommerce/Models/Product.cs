namespace ECommerce.Models
{
    public class Product
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public int CategoryID { get; set; }
        public int Stock { get; set; }
        public double Price { get; set; }
        public string Description { get; set; }

        // Yeni özellik: Resim URL'si
        public string ImageUrl { get; set; } = "/resim/";


        // Nullable Navigation Property
        public virtual Category? Category { get; set; } // Nullable yap
    }
}
