using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopProject.Models;
using ShopProject.Models.Entities;

namespace ShopProject.Controllers;

public class HomeController : Controller
{
    private readonly ClothingShopDbContext _context;

    public HomeController(ClothingShopDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var currentYear = today.Year;
        var currentMonth = today.Month;
        var monthStart = new DateTime(currentYear, currentMonth, 1);
        var monthEnd = monthStart.AddMonths(1);
        var prevMonthStart = monthStart.AddMonths(-1);

        var validOrdersYear = await _context.DonHangs
            .Where(d => d.ThoiGianTao.Year == currentYear && d.TrangThai != "Da_huy")
            .Select(d => new { d.ThoiGianTao, d.TongThanhToan })
            .ToListAsync();

        var monthRevenue = validOrdersYear
            .Where(d => d.ThoiGianTao >= monthStart && d.ThoiGianTao < monthEnd)
            .Sum(d => d.TongThanhToan);

        var prevMonthRevenue = validOrdersYear
            .Where(d => d.ThoiGianTao >= prevMonthStart && d.ThoiGianTao < monthStart)
            .Sum(d => d.TongThanhToan);

        var monthOrderCount = validOrdersYear
            .Count(d => d.ThoiGianTao >= monthStart && d.ThoiGianTao < monthEnd);

        var tonKhos = await _context.TonKhos
    .Include(t => t.BienThe)
        .ThenInclude(bt => bt.SanPham)
    .Where(t => t.Kho.TrangThai && t.BienThe.TrangThai && t.BienThe.SanPham.TrangThai == "Dang_ban")
    .ToListAsync();

var variantIds = tonKhos.Select(t => t.BienTheId).Distinct().ToList();

var inCartByVariant = await _context.GioHangChiTiets
    .Where(ct => variantIds.Contains(ct.BienTheId))
    .GroupBy(ct => ct.BienTheId)
    .Select(g => new { BienTheId = g.Key, Qty = g.Sum(x => x.SoLuong) })
    .ToDictionaryAsync(x => x.BienTheId, x => x.Qty);

var since30Days = DateTime.Now.AddDays(-30);
var soldByVariant = await _context.DonHangChiTiets
    .Where(ct => variantIds.Contains(ct.BienTheId)
                 && ct.DonHang.ThoiGianTao >= since30Days
                 && ct.DonHang.TrangThai != "Da_huy")
    .GroupBy(ct => ct.BienTheId)
    .Select(g => new { BienTheId = g.Key, Qty = g.Sum(x => x.SoLuong) })
    .ToDictionaryAsync(x => x.BienTheId, x => x.Qty);

var lowStockCount = tonKhos.Count(t =>
{
    var avail = t.SoLuongTon - t.SoLuongGiuCho;
    var inCart = inCartByVariant.GetValueOrDefault(t.BienTheId, 0);
    var sold30 = soldByVariant.GetValueOrDefault(t.BienTheId, 0);
    var isHighDemand = (inCart + t.SoLuongGiuCho >= 2) || (sold30 >= 3 && avail <= 8);
    return avail <= 5 || isHighDemand;
});

        var customerCount = await _context.NguoiDungs
            .CountAsync(u => u.VaiTros.Any(v => v.MaVaiTro == "KHACH_HANG"));

        var monthlyRevenues = Enumerable.Range(1, 12)
            .Select(m => validOrdersYear.Where(d => d.ThoiGianTao.Month == m).Sum(d => d.TongThanhToan))
            .ToList();

        var catSales = await _context.DonHangChiTiets
            .Where(ct => ct.DonHang.ThoiGianTao.Year == currentYear && ct.DonHang.TrangThai != "Da_huy")
            .GroupBy(ct => ct.BienThe.SanPham.DanhMuc.TenDanhMuc)
            .Select(g => new { Category = g.Key, Revenue = g.Sum(x => x.DonGia * x.SoLuong) })
            .OrderByDescending(x => x.Revenue)
            .ToListAsync();

        var totalCatRev = catSales.Sum(x => x.Revenue);
        var categoryBreakdown = catSales.Select(c => new
        {
            c.Category,
            c.Revenue,
            Percent = totalCatRev > 0 ? (int)Math.Round(c.Revenue * 100m / totalCatRev) : 0
        }).ToList();

        var recentProducts = await _context.SanPhams
            .Include(sp => sp.DanhMuc)
            .Include(sp => sp.SanPhamAnhs)
            .Include(sp => sp.BienTheSanPhams)
                .ThenInclude(bt => bt.TonKhos)
            .OrderByDescending(sp => sp.ThoiGianTao)
            .Take(5)
            .ToListAsync();

        var recentProductVms = recentProducts.Select(sp =>
        {
            var avail = sp.BienTheSanPhams
                .Where(bt => bt.TrangThai)
                .Sum(bt => bt.TonKhos.Sum(tk => tk.SoLuongTon - tk.SoLuongGiuCho));
            var st = sp.TrangThai != "Dang_ban"
                ? "Ngừng bán"
                : (avail == 0 ? "Hết hàng" : (avail <= 5 ? "Sắp hết" : "Đang bán"));

            return new ProductViewModel
            {
                Id = sp.Id,
                Name = sp.TenSanPham,
                Category = sp.DanhMuc?.TenDanhMuc ?? "Khác",
                Price = sp.GiaBan,
                Stock = avail,
                Status = st,
                Image = sp.SanPhamAnhs
                    .OrderByDescending(a => a.LaAnhDaiDien)
                    .Select(a => a.Url)
                    .FirstOrDefault() ?? "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=200"
            };
        }).ToList();

        var latestOrders = await _context.DonHangs
            .OrderByDescending(d => d.ThoiGianTao)
            .Take(5)
            .ToListAsync();

        ViewBag.CurrentYear = currentYear;
        ViewBag.CurrentMonth = currentMonth;
        ViewBag.MonthRevenue = monthRevenue;
        ViewBag.PrevMonthRevenue = prevMonthRevenue;
        ViewBag.MonthOrderCount = monthOrderCount;
        ViewBag.LowStockCount = lowStockCount;
        ViewBag.CustomerCount = customerCount;
        ViewBag.MonthlyRevenues = monthlyRevenues;
        ViewBag.CategoryBreakdown = categoryBreakdown;
        ViewBag.LatestOrders = latestOrders;

        return View(recentProductVms);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}