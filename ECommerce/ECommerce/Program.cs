using ECommerce;
using ECommerce.Controllers;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Session'ý yapýlandýr
builder.Services.AddDistributedMemoryCache(); // Session için gerekli
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Oturum süresi (30 dakika)
    options.Cookie.HttpOnly = true; // Tarayýcý tarafýndan sadece HTTP üzerinden eriþilebilir
    options.Cookie.IsEssential = true; // GDPR uyumluluðu için gerekli
});

// Add services to the container.
builder.Services.AddControllersWithViews();

// SignalR'ý servislere ekle
builder.Services.AddSignalR();

builder.Services.AddHttpContextAccessor();


// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ApplicationDbContext'i Dependency Injection'da scoped olarak tanýmlayýn
builder.Services.AddScoped<ApplicationDbContext>();

var app = builder.Build();

// Global ServiceProvider
Program.ServiceProvider = app.Services; // <-- Burada atama yapýlýyor

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// SignalR hub'ýný haritaya ekle
app.MapHub<ECommerce.Hubs.OrderHub>("/orderHub"); // SignalR hub endpoint'i

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.UseSession(); // Session middleware ekle

//using (var scope = app.Services.CreateScope())
//{
  //  var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
   // var accountController = new AccountController(context);

    //accountController.GenerateRandomCustomers();
//}

app.Run();

public partial class Program
{
    public static IServiceProvider ServiceProvider { get; set; }
}
