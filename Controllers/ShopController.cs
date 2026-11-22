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
            if (!string.IsNullOrWhiteSpace(email))
                return email;

            const string cookieName = "fashionhub_customer";
            if (Request.Cookies.TryGetValue(cookieName, out var visitorKey) && !string.IsNullOrWhiteSpace(visitorKey))
                return visitorKey;

            visitorKey = $"Guest-{Guid.NewGuid():N}";
            Response.Cookies.Append(cookieName, visitorKey, new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                Expires = DateTimeOffset.Now.AddDays(30)
            });
            return visitorKey;
        }
    }

    private static void UpdateHistory(Dictionary<string, List<ProductViewModel>> store, string key, ProductViewModel product)
    {
        if (!store.TryGetValue(key, out var history))
        {
            history = [];
            store[key] = history;
        }
        history.RemoveAll(p => p.Id == product.Id);
        history.Insert(0, product);
    }

    private List<ProductViewModel> BuildRecommendations(int take = 4)
    {
        var key = CustomerKey;
        var scores = Products.ToDictionary(p => p.Id, _ => 0d);

        AddWeightedHistory(scores, key, ShopDataStore.PurchaseHistory, 6);
        AddWeightedHistory(scores, key, ShopDataStore.WishlistHistory, 5);
        AddWeightedHistory(scores, key, ShopDataStore.CartHistory, 4);
        AddWeightedHistory(scores, key, ShopDataStore.ViewedHistory, 2);

        if (ShopDataStore.SearchHistory.TryGetValue(key, out var searches))
        {
            foreach (var search in searches.Take(8))
            {
                foreach (var product in Products.Where(p =>
                    p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    p.Category.Contains(search, StringComparison.OrdinalIgnoreCase)))
                    scores[product.Id] += 3;
            }
        }

        if (scores.Values.All(score => score == 0))
        {
            var popularIds = new[] { 2, 1, 5, 3, 6, 4 };
            var coldStart = popularIds
                .Select(id => Products.FirstOrDefault(p => p.Id == id))
                .Where(p => p != null)
                .Cast<ProductViewModel>()
                .Where(p => p.Stock > 0)
                .Take(take)
                .ToList();
            if (coldStart.Count >= take)
                return coldStart;
        }

        var candidates = scores
            .Select(entry =>
            {
                var product = Products.First(p => p.Id == entry.Key);
                var stockBonus = product.Stock > 0 ? 0.5 : -10;
                var recentPurchase = ShopDataStore.PurchaseHistory.TryGetValue(key, out var purchased) &&
                                     purchased.Take(3).Any(p => p.Id == product.Id);
                return (Product: product, Score: entry.Value + stockBonus - (recentPurchase ? 1 : 0));
            })
            .Where(item => item.Product.Stock > 0)
            .OrderByDescending(item => item.Score)
            .ThenByDescending(item => item.Product.Id % 4)
            .ToList();

        var selected = new List<ProductViewModel>();
        var categoryCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in candidates)
        {
            categoryCounts.TryGetValue(candidate.Product.Category, out var count);
            if (count >= 2)
                continue;

            selected.Add(candidate.Product);
            categoryCounts[candidate.Product.Category] = count + 1;
            if (selected.Count == take)
                break;
        }

        foreach (var candidate in candidates)
        {
            if (selected.Count == take)
                break;
            if (!selected.Contains(candidate.Product))
                selected.Add(candidate.Product);
        }

        return selected.Take(take).ToList();
    }

    private static void AddWeightedHistory(
        Dictionary<int, double> scores,
        string key,
        Dictionary<string, List<ProductViewModel>> store,
        double weight)
    {
        if (!store.TryGetValue(key, out var history))
            return;

        for (var i = 0; i < history.Count; i++)
        {
            var product = history[i];
            if (scores.ContainsKey(product.Id))
                scores[product.Id] += weight - i * 0.15;

            foreach (var sameCategory in Products.Where(p => p.Category.Equals(product.Category, StringComparison.OrdinalIgnoreCase)))
                scores[sameCategory.Id] += (weight * 0.35) - i * 0.05;
        }
    }

    public IActionResult Index(string? search, string? category)
    {
        var items = Products.Where(p => (string.IsNullOrEmpty(search) || p.Name.Contains(search, StringComparison.OrdinalIgnoreCase)) && (string.IsNullOrEmpty(category) || p.Category == category)).ToList();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var key = CustomerKey;
            if (!ShopDataStore.SearchHistory.TryGetValue(key, out var searches))
            {
                searches = [];
                ShopDataStore.SearchHistory[key] = searches;
            }
            searches.RemoveAll(value => value.Equals(search.Trim(), StringComparison.OrdinalIgnoreCase));
            searches.Insert(0, search.Trim());
        }

        var recommendations = BuildRecommendations();

        return View(new StoreViewModel
        {
            Products = items,
            Recommendations = recommendations,
            Categories = ShopDataStore.Categories
        });
    }

    public IActionResult Cart() => View(Products.Take(2).ToList());
    public IActionResult Wishlist()
    {
        var items = ShopDataStore.WishlistHistory.TryGetValue(CustomerKey, out var saved)
            ? saved.Where(p => p.Stock > 0).ToList()
            : [];
        return View(items);
    }
    public IActionResult Checkout() => View(Products.Take(2).ToList());

    public IActionResult Detail(int id)
    {
        var product = Products.FirstOrDefault(p => p.Id == id) ?? Products.First();
        UpdateHistory(ShopDataStore.ViewedHistory, CustomerKey, product);

        var complementaryCategories = new[]
        {
            ("Áo sơ mi", new[] { "Quần", "Váy đầm", "Áo khoác" }),
            ("Áo thun", new[] { "Quần", "Áo khoác", "Váy đầm" }),
            ("Quần", new[] { "Áo thun", "Áo sơ mi", "Áo khoác" }),
            ("Váy đầm", new[] { "Áo khoác", "Áo sơ mi", "Áo thun" }),
            ("Áo khoác", new[] { "Quần", "Váy đầm", "Áo thun" })
        }.FirstOrDefault(pair => pair.Item1 == product.Category).Item2 ?? [];

        var related = Products
            .Where(p => p.Id != product.Id && p.Stock > 0)
            .OrderByDescending(p => p.Category == product.Category)
            .ThenByDescending(p => complementaryCategories.Contains(p.Category))
            .ThenBy(p => Math.Abs(p.Price - product.Price))
            .Take(4)
            .ToList();
        ViewBag.Related = related;
        return View(product);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public IActionResult RecordInteraction(int id, string type)
    {
        var product = Products.FirstOrDefault(p => p.Id == id);
        if (product == null)
            return NotFound();

        var key = CustomerKey;
        switch (type)
        {
            case "cart":
                UpdateHistory(ShopDataStore.CartHistory, key, product);
                break;
            case "wishlist":
                UpdateHistory(ShopDataStore.WishlistHistory, key, product);
                break;
            case "wishlist_remove":
                if (ShopDataStore.WishlistHistory.TryGetValue(key, out var wishlist))
                    wishlist.RemoveAll(p => p.Id == product.Id);
                break;
            default:
                return BadRequest();
        }
        return Ok();
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
