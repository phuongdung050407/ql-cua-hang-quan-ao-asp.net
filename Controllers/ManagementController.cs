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
    public IActionResult Employees() => View();

    [Authorize(Roles = "Admin")]
    public IActionResult Users() => View();

    public IActionResult Orders() => View();

    public IActionResult Inventory() => View();

    [Authorize(Roles = "Admin")]
    public IActionResult Promotions() => View();

    public IActionResult Reviews() => View();

    [Authorize(Roles = "Admin")]
    public IActionResult Reports() => View();
}
