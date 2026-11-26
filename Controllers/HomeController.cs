using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopProject.Models;

namespace ShopProject.Controllers;

[Authorize(Roles = "Admin,Employee")]
public class HomeController : Controller
{
    private readonly ClothingShopContext _context;

    public HomeController(ClothingShopContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var isAdmin = User.IsInRole("Admin");

        decimal revenueToday = 0;
        decimal revenueMonth = 0;
        int customerCount = 0;

        // Chỉ truy vấn dữ liệu doanh thu & khách hàng khi tài khoản là Admin
        if (isAdmin)
        {
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
            var validOrders = _context.DonHangs.Where(d => d.TrangThai != "Da_huy");

            revenueToday = await validOrders
                .Where(d => d.ThoiGianTao >= today)
                .SumAsync(d => (decimal?)d.TongThanhToan) ?? 0;

            revenueMonth = await validOrders
                .Where(d => d.ThoiGianTao >= firstDayOfMonth)
                .SumAsync(d => (decimal?)d.TongThanhToan) ?? 0;

            customerCount = await _context.NguoiDungs.CountAsync();
        }

        // Dữ liệu vận hành chung cho cả Admin và Nhân viên
        var ordersToday = await _context.DonHangs
            .CountAsync(d => d.ThoiGianTao >= today);

        var lowStockCount = await _context.TonKhos
            .CountAsync(t => t.SoLuongTon <= 5);

        var dbProducts = await _context.SanPhams
            .Include(p => p.DanhMuc)
            .Include(p => p.SanPhamAnhs)
            .Include(p => p.BienTheSanPhams).ThenInclude(bt => bt.TonKhos)
            .OrderByDescending(p => p.Id)
            .Take(6)
            .ToListAsync();

        var products = dbProducts.Select(p =>
        {
            var stock = p.BienTheSanPhams.SelectMany(bt => bt.TonKhos).Sum(t => t.SoLuongTon);
            var status = stock == 0 ? "Hết hàng" : stock <= 5 ? "Sắp hết" : "Đang bán";
            var img = p.SanPhamAnhs.FirstOrDefault()?.Url;
            if (string.IsNullOrWhiteSpace(img) || !img.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                img = "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=300";
            }

            return new ProductViewModel
            {
                Id = p.Id,
                Name = p.TenSanPham,
                Category = p.DanhMuc?.TenDanhMuc ?? "Khác",
                Price = p.GiaBan,
                Stock = stock,
                Status = status,
                Image = img
            };
        }).ToList();

        var dbOrders = await _context.DonHangs
            .OrderByDescending(d => d.ThoiGianTao)
            .Take(6)
            .ToListAsync();

        var recentOrders = dbOrders.Select(o => new OrderViewModel
        {
            Code = o.MaDonHang,
            Customer = o.TenNguoiNhan,
            Date = o.ThoiGianTao.ToString("dd/MM/yyyy"),
            Total = o.TongThanhToan,
            Status = MapOrderStatusToVn(o.TrangThai)
        }).ToList();

        var model = new DashboardViewModel
        {
            RevenueToday = revenueToday,
            RevenueMonth = revenueMonth,
            OrdersToday = ordersToday,
            LowStockCount = lowStockCount,
            CustomerCount = customerCount,
            Products = products,
            RecentOrders = recentOrders
        };

        return View(model);
    }

    private static string MapOrderStatusToVn(string dbStatus) => dbStatus switch
    {
        "Cho_xac_nhan" => "Chờ xử lý",
        "Da_xac_nhan" => "Đã xác nhận",
        "Dang_giao" => "Đang giao",
        "Da_giao" => "Hoàn tất",
        "Da_huy" => "Đã hủy",
        _ => "Chờ xử lý"
    };

    public IActionResult Privacy() => View();

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}