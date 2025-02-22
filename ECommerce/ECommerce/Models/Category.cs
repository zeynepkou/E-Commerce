namespace ECommerce.Models
{
    public class Category
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
        public int? ParentCategoryID { get; set; } // Üst kategori için Foreign Key (nullable)

        // Navigation Properties
        public virtual ICollection<Product> Products { get; set; }
        public virtual Category ParentCategory { get; set; }
        public virtual ICollection<Category> SubCategories { get; set; }
    }
}
