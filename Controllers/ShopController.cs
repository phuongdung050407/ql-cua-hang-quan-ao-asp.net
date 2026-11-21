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

    private static List<ProductViewModel> Products => ShopDataStore.Products;

    private string CustomerKey
    {
        get
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            return !string.IsNullOrWhiteSpace(email) ? email : "Guest";
        }
    }

    public IActionResult Index(string? search, string? category)
    {
        var items = Products.Where(p => (string.IsNullOrEmpty(search) || p.Name.Contains(search, StringComparison.OrdinalIgnoreCase)) && (string.IsNullOrEmpty(category) || p.Category == category)).ToList();

        var preferredCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (ShopDataStore.PurchaseHistory.TryGetValue(CustomerKey, out var purchased))
            purchased.Take(10).ToList().ForEach(p => preferredCategories.Add(p.Category));
        if (ShopDataStore.ViewedHistory.TryGetValue(CustomerKey, out var viewed))
            viewed.Take(10).ToList().ForEach(p => preferredCategories.Add(p.Category));

        var recommendations = preferredCategories.Count > 0
            ? Products.Where(p => preferredCategories.Contains(p.Category)).Concat(Products.Where(p => !preferredCategories.Contains(p.Category))).DistinctBy(p => p.Id).Take(4).ToList()
            : Products.Take(4).ToList();

        return View(new StoreViewModel
        {
            Products = items,
            Recommendations = recommendations,
            Categories = ShopDataStore.Categories
        });
    }

    public IActionResult Cart() => View(Products.Take(2).ToList());
    public IActionResult Checkout() => View(Products.Take(2).ToList());

    public IActionResult Detail(int id)
    {
        var product = Products.FirstOrDefault(p => p.Id == id) ?? Products.First();

        if (!ShopDataStore.ViewedHistory.TryGetValue(CustomerKey, out var history))
        {
            history = [];
            ShopDataStore.ViewedHistory[CustomerKey] = history;
        }
        history.RemoveAll(p => p.Id == product.Id);
        history.Insert(0, product);

        var related = Products.Where(p => p.Id != product.Id)
            .OrderByDescending(p => p.Category == product.Category)
            .ThenByDescending(p => p.Id)
            .Take(4).ToList();
        ViewBag.Related = related;
        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult PlaceOrder(string fullName, string phone, string email, string address, string payment)
    {
        var code = "DH-" + DateTime.Now.ToString("yyMMddHHmm");
        var items = Products.Take(2).Select(p => new ProductViewModel
        {
            Id=p.Id, Name=p.Name, Category=p.Category, Price=p.Price, Stock=p.Stock, Status=p.Status, Image=p.Image
        }).ToList();
        var total = items.Sum(p => p.Price);

        var customerName = !string.IsNullOrWhiteSpace(fullName) ? fullName.Trim()
            : (User.Identity?.IsAuthenticated == true ? User.Identity.Name : "Khách hàng") ?? "Khách hàng";
        var customerEmail = !string.IsNullOrWhiteSpace(email) ? email.Trim()
            : User.FindFirstValue(ClaimTypes.Email) ?? "Guest";

        var customerOrder = new CustomerOrderViewModel
        {
            Code = code,
            CustomerName = customerName,
            Email = customerEmail,
            Phone = phone ?? "",
            Address = address ?? "",
            PaymentMethod = payment == "Bank" ? "Chuyển khoản ngân hàng" : "Thanh toán khi nhận hàng (COD)",
            Status = "Chờ xác nhận",
            CreatedAt = DateTime.Now,
            Total = total,
            Items = items
        };
        ShopDataStore.CustomerOrders.Insert(0, customerOrder);

        if (!ShopDataStore.PurchaseHistory.TryGetValue(customerEmail, out var purchases))
        {
            purchases = [];
            ShopDataStore.PurchaseHistory[customerEmail] = purchases;
        }
        foreach (var item in items)
        {
            purchases.RemoveAll(p => p.Id == item.Id);
            purchases.Insert(0, item);
        }

        ShopDataStore.Orders.Insert(0, new OrderViewModel
        {
            Code=code, Customer=customerName, Date=DateTime.Now.ToString("dd/MM/yyyy"), Total=total, Status="Chờ xử lý"
        });

        TempData["OrderCode"] = code;
        TempData["Success"] = "Đặt hàng thành công. Gợi ý sản phẩm đã được cập nhật.";
        return RedirectToAction(nameof(OrderSuccess));
    }

    public IActionResult OrderSuccess()
    {
        ViewBag.OrderCode = TempData["OrderCode"]?.ToString() ?? "DH-261119001";
        return View(Products.Take(2).ToList());
    }

    public IActionResult Invoice(string id)
    {
        var order = ShopDataStore.CustomerOrders.FirstOrDefault(o => o.Code == id);
        if (order != null)
        {
            ViewBag.OrderCode = order.Code;
            ViewBag.CustomerOrder = order;
            return View(order.Items);
        }
        ViewBag.OrderCode = string.IsNullOrWhiteSpace(id) ? "DH-261119001" : id;
        return View(Products.Take(2).ToList());
    }

    [Authorize]
    public IActionResult OrderHistory()
    {
        var key = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? "Guest";
        var orders = ShopDataStore.CustomerOrders
            .Where(o => o.Email.Equals(key, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return View(orders);
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
