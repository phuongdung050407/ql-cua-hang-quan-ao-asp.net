using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopProject.Models;

namespace ShopProject.Controllers;

[Authorize(Roles = "Admin,Employee")]
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
            LowStockCount = ShopDataStore.Products.Count(p => p.Stock <= 5),
            CustomerCount = ShopDataStore.Users.Count,
            Products = ShopDataStore.Products.Take(6).ToList(),
            RecentOrders = ShopDataStore.Orders.Take(3).ToList()
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
