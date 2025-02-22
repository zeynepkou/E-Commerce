using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Models
{
    public class Admin
    {
        public int AdminID { get; set; }
        public string AdminLoginName { get; set; }
        public string AdminPassword { get; set; }
        public string AdminName { get; set; }
    }
}

