using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopProject.Models;

namespace ShopProject.Controllers;

[Authorize(Roles = "Admin,Employee")]
public class ManagementController : Controller
{
    [Authorize(Roles = "Admin")]
    public IActionResult Products() => View();

    [Authorize(Roles = "Admin")]
    public IActionResult Categories() => View();

    [Authorize(Roles = "Admin")]
    public IActionResult Employees() => View(ShopDataStore.Employees);

    [Authorize(Roles = "Admin")]
    public IActionResult Users() => View(ShopDataStore.Users);

    public IActionResult Orders() => View();

    public IActionResult Inventory() => View();

    [Authorize(Roles = "Admin")]
    public IActionResult Promotions() => View();

    public IActionResult Reviews() => View();

    [Authorize(Roles = "Admin")]
    public IActionResult Reports() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public IActionResult AddProduct(string name, string category, decimal price, int stock, string image)
    {
        if (string.IsNullOrWhiteSpace(name)) return RedirectToAction(nameof(Products));
        ShopDataStore.Products.Add(new ProductViewModel
        {
            Id = ShopDataStore.Products.Count == 0 ? 1 : ShopDataStore.Products.Max(p => p.Id) + 1,
            Name = name.Trim(), Category = category, Price = price, Stock = stock,
            Status = stock <= 0 ? "Hết hàng" : stock <= 5 ? "Sắp hết" : "Đang bán",
            Image = string.IsNullOrWhiteSpace(image) ? "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=600" : image
        });
        TempData["Success"] = "Đã thêm sản phẩm thành công.";
        return RedirectToAction(nameof(Products));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public IActionResult DeleteProduct(int id)
    {
        var product = ShopDataStore.Products.FirstOrDefault(p => p.Id == id);
        if (product != null) ShopDataStore.Products.Remove(product);
        TempData["Success"] = "Đã xóa sản phẩm.";
        return RedirectToAction(nameof(Products));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Employee")]
    public IActionResult UpdateOrderStatus(string code, string status)
    {
        var order = ShopDataStore.Orders.FirstOrDefault(o => o.Code == code);
        if (order != null) order.Status = status;
        TempData["Success"] = "Đã cập nhật trạng thái đơn hàng.";
        return RedirectToAction(nameof(Orders));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Employee")]
    public IActionResult UpdateInventory(int id, int stock)
    {
        var item = ShopDataStore.Inventory.FirstOrDefault(i => i.Id == id);
        if (item != null) item.Stock = Math.Max(0, stock);
        var product = ShopDataStore.Products.FirstOrDefault(p => p.Name == item?.Name);
        if (product != null)
        {
            product.Stock = Math.Max(0, stock);
            product.Status = stock <= 0 ? "Hết hàng" : stock <= 5 ? "Sắp hết" : "Đang bán";
        }
        TempData["Success"] = "Đã cập nhật tồn kho.";
        return RedirectToAction(nameof(Inventory));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Employee")]
    public IActionResult UpdateStatus(string type, int id, string status)
    {
        if (type == "employee")
        {
            var person = ShopDataStore.Employees.FirstOrDefault(p => p.Id == id);
            if (person != null) person.Status = status;
        }
        if (type == "user")
        {
            var person = ShopDataStore.Users.FirstOrDefault(p => p.Id == id);
            if (person != null) person.Status = status;
        }
        TempData["Success"] = "Đã cập nhật trạng thái.";
        return Redirect(Request.Headers["Referer"].ToString() is string referer && referer.Length > 0 ? referer : "/Management/Orders");
    }
}
