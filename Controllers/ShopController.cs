using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopProject.Models;

namespace ShopProject.Controllers;

public class ShopController : Controller
{
    private static readonly Dictionary<string, (string Password, string Name, string Role)> DemoUsers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["admin@fashion.vn"] = ("Admin123@", "Trần Minh", "Admin"),
        ["nhanvien@fashion.vn"] = ("Nhanvien123@", "Nguyễn Lan", "Employee"),
        ["khachhang@gmail.com"] = ("Khachhang123@", "Nguyễn Minh Anh", "Customer")
    };

    private static readonly ConcurrentDictionary<string, (string Password, string Name, string Role)> RegisteredUsers = new();

    private static readonly List<ProductViewModel> Products =
    [
        new() { Id=1, Name="Áo sơ mi linen trắng", Category="Áo sơ mi", Price=450000, Stock=20, Status="Còn hàng", Image="https://images.unsplash.com/photo-1602810318383-e386cc2a3ccf?w=600" },
        new() { Id=2, Name="Váy hoa midi", Category="Váy đầm", Price=680000, Stock=14, Status="Còn hàng", Image="https://images.unsplash.com/photo-1595777457583-95e059d581b8?w=600" },
        new() { Id=3, Name="Quần jean nữ slim", Category="Quần", Price=550000, Stock=9, Status="Còn hàng", Image="https://images.unsplash.com/photo-1542272604-787c3835535d?w=600" },
        new() { Id=4, Name="Áo khoác denim", Category="Áo khoác", Price=890000, Stock=7, Status="Còn hàng", Image="https://images.unsplash.com/photo-1523205771623-e0faa4d2813d?w=600" },
        new() { Id=5, Name="Đầm suông mùa hè", Category="Váy đầm", Price=720000, Stock=11, Status="Còn hàng", Image="https://images.unsplash.com/photo-1566174053879-31528523f8ae?w=600" },
        new() { Id=6, Name="Áo thun basic", Category="Áo thun", Price=290000, Stock=25, Status="Còn hàng", Image="https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=600" }
    ];

    public IActionResult Index(string? search, string? category)
    {
        var items = Products.Where(p => (string.IsNullOrEmpty(search) || p.Name.Contains(search, StringComparison.OrdinalIgnoreCase)) && (string.IsNullOrEmpty(category) || p.Category == category)).ToList();
        return View(new StoreViewModel
        {
            Products = items,
            Categories =
            [
                new() { Id=1, Name="Áo sơ mi", ProductCount=18 }, new() { Id=2, Name="Áo thun", ProductCount=24 },
                new() { Id=3, Name="Quần", ProductCount=16 }, new() { Id=4, Name="Váy đầm", ProductCount=21 }
            ]
        });
    }

    public IActionResult Cart() => View(Products.Take(2).ToList());
    public IActionResult Checkout() => View(Products.Take(2).ToList());

    public IActionResult Detail(int id)
    {
        var product = Products.FirstOrDefault(p => p.Id == id) ?? Products.First();
        ViewBag.Related = Products.Where(p => p.Id != product.Id).Take(4).ToList();
        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult PlaceOrder()
    {
        TempData["OrderCode"] = "DH-" + DateTime.Now.ToString("yyMMddHHmm");
        return RedirectToAction(nameof(OrderSuccess));
    }

    public IActionResult OrderSuccess()
    {
        ViewBag.OrderCode = TempData["OrderCode"]?.ToString() ?? "DH-261119001";
        return View(Products.Take(2).ToList());
    }

    public IActionResult Invoice(string id)
    {
        ViewBag.OrderCode = string.IsNullOrWhiteSpace(id) ? "DH-261119001" : id;
        return View(Products.Take(2).ToList());
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var email = model.Email.Trim();
        if (DemoUsers.ContainsKey(email) || !RegisteredUsers.TryAdd(email, (model.Password, model.FullName, "Customer")))
        {
            ModelState.AddModelError("Email", "Email đã được sử dụng.");
            return View(model);
        }
        TempData["RegisterSuccess"] = "Đăng ký thành công. Hãy đăng nhập.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Login() => View(new LoginViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var email = model.Email.Trim();
        var account = DemoUsers.TryGetValue(email, out var demo) ? demo : (RegisteredUsers.TryGetValue(email, out var registered) ? registered : default);
        if (account.Password == null || account.Password != model.Password)
        {
            ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
            return View(model);
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, account.Name),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, account.Role)
        };
        var identity = new ClaimsIdentity(claims, "ShopCookie", ClaimTypes.Name, ClaimTypes.Role);
        await HttpContext.SignInAsync("ShopCookie", new ClaimsPrincipal(identity), new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8)
        });

        return account.Role switch
        {
            "Admin" => RedirectToAction("Index", "Home"),
            "Employee" => RedirectToAction("Orders", "Management"),
            _ => RedirectToAction(nameof(Index))
        };
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("ShopCookie");
        return RedirectToAction(nameof(Index));
    }

    public IActionResult AccessDenied() => View();
}
