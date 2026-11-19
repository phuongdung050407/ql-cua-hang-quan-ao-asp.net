using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ShopProject.Models;

namespace ShopProject.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        var model = new DashboardViewModel
        {
            RevenueToday = 12_450_000,
            RevenueMonth = 248_600_000,
            OrdersToday = 28,
            LowStockCount = 5,
            CustomerCount = 1248,
            Products =
            [
                new() { Id = 1, Name = "Áo sơ mi trắng", Category = "Áo sơ mi", Price = 450000, Stock = 12, Status = "Đang bán", Image = "https://images.unsplash.com/photo-1602810318383-e386cc2a3ccf?w=300" },
                new() { Id = 2, Name = "Váy hoa mùa hè", Category = "Váy đầm", Price = 680000, Stock = 8, Status = "Đang bán", Image = "https://images.unsplash.com/photo-1595777457583-95e059d581b8?w=300" },
                new() { Id = 3, Name = "Quần jean slim fit", Category = "Quần", Price = 550000, Stock = 3, Status = "Sắp hết", Image = "https://images.unsplash.com/photo-1542272604-787c3835535d?w=300" },
                new() { Id = 4, Name = "Áo khoác denim", Category = "Áo khoác", Price = 890000, Stock = 0, Status = "Hết hàng", Image = "https://images.unsplash.com/photo-1523205771623-e0faa4d2813d?w=300" }
            ],
            RecentOrders =
            [
                new() { Code = "DH-26011", Customer = "Nguyễn Minh Anh", Date = "19/11/2026", Total = 1450000, Status = "Hoàn tất" },
                new() { Code = "DH-26010", Customer = "Trần Quốc Bảo", Date = "19/11/2026", Total = 890000, Status = "Đang giao" },
                new() { Code = "DH-26009", Customer = "Lê Thu Hà", Date = "18/11/2026", Total = 2240000, Status = "Chờ xử lý" }
            ]
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
