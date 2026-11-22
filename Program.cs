using Microsoft.EntityFrameworkCore;
using ShopProject.Models;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình Cookie Authentication
builder.Services.AddAuthentication("ShopCookie")
    .AddCookie("ShopCookie", options =>
    {
        options.LoginPath = "/Shop/Login";
        options.AccessDeniedPath = "/Shop/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Add services to the container.
builder.Services.AddControllersWithViews();

// ...
builder.Services.AddDbContext<ClothingShopContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

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

app.UseAuthentication(); // Thêm dòng này để sử dụng Authentication
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
