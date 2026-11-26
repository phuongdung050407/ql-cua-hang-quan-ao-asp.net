using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopProject.Models;
using System.Security.Claims;

namespace ShopProject.Controllers;

[Authorize(Roles = "Admin,Employee")]
public class ManagementController : Controller
{
    private readonly ClothingShopContext _context;

    public ManagementController(ClothingShopContext context)
    {
        _context = context;
    }

    // 1. SẢN PHẨM & TỒN KHO THẬT (Chỉ Admin)
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Products()
    {
        var dbProducts = await _context.SanPhams
            .Include(p => p.DanhMuc)
            .Include(p => p.SanPhamAnhs)
            .Include(p => p.BienTheSanPhams)
                .ThenInclude(bt => bt.TonKhos)
            .OrderByDescending(p => p.Id)
            .ToListAsync();

        var list = dbProducts.Select(p =>
        {
            var totalStock = p.BienTheSanPhams.SelectMany(bt => bt.TonKhos).Sum(tk => tk.SoLuongTon);
            return new ProductViewModel
            {
                Id = p.Id,
                Name = p.TenSanPham,
                Category = p.DanhMuc?.TenDanhMuc ?? "Khác",
                Price = p.GiaBan,
                Stock = totalStock,
                Status = totalStock == 0 ? "Hết hàng" : (totalStock <= 10 ? "Sắp hết" : "Đang bán"),
                Image = p.SanPhamAnhs.FirstOrDefault()?.Url ?? "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=600"
            };
        }).ToList();

        return View(list);
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateProduct()
    {
        ViewBag.Categories = await _context.DanhMucs.Where(c => c.TrangThai).ToListAsync();
        return View(new ProductViewModel());
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProduct(ProductViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Name) || model.Price <= 0)
        {
            ViewBag.Categories = await _context.DanhMucs.Where(c => c.TrangThai).ToListAsync();
            return View(model);
        }

        var danhMuc = await _context.DanhMucs.FirstOrDefaultAsync(c => c.TenDanhMuc == model.Category)
                      ?? await _context.DanhMucs.FirstAsync();

        var maSp = "SP" + DateTime.Now.ToString("yyMMddHHmmss");
        var sp = new SanPham
        {
            MaSanPham = maSp,
            TenSanPham = model.Name.Trim(),
            DanhMucId = danhMuc.Id,
            GiaGoc = model.Price,
            GiaBan = model.Price,
            TrangThai = "Dang_ban",
            ThoiGianTao = DateTime.Now,
            ThoiGianCapNhat = DateTime.Now
        };
        _context.SanPhams.Add(sp);
        await _context.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(model.Image))
        {
            _context.SanPhamAnhs.Add(new SanPhamAnh
            {
                SanPhamId = sp.Id,
                Url = model.Image.Trim(),
                LaAnhDaiDien = true
            });
        }

        var size = await _context.KichThuocs.FirstOrDefaultAsync();
        var color = await _context.MauSacs.FirstOrDefaultAsync();
        var kho = await _context.Khos.FirstOrDefaultAsync();

        if (size != null && color != null)
        {
            var bienThe = new BienTheSanPham
            {
                SanPhamId = sp.Id,
                Sku = $"{maSp}-{size.MaSize}",
                KichThuocId = size.Id,
                MauSacId = color.Id,
                GiaBan = model.Price,
                TrangThai = true
            };
            _context.BienTheSanPhams.Add(bienThe);
            await _context.SaveChangesAsync();

            if (kho != null)
            {
                _context.TonKhos.Add(new TonKho
                {
                    KhoId = kho.Id,
                    BienTheId = bienThe.Id,
                    SoLuongTon = Math.Max(0, model.Stock),
                    SoLuongGiuCho = 0
                });
            }
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Products));
    }

    // Xem chi tiết sản phẩm

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ViewDetails(int id)
    {
        var p = await _context.SanPhams
            .Include(x => x.DanhMuc)
            .Include(x => x.SanPhamAnhs)
            .Include(x => x.BienTheSanPhams)
                .ThenInclude(bt => bt.KichThuoc)
            .Include(x => x.BienTheSanPhams)
                .ThenInclude(bt => bt.MauSac)
            .Include(x => x.BienTheSanPhams)
                .ThenInclude(bt => bt.TonKhos)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (p == null)
            return NotFound();

        var totalStock = p.BienTheSanPhams
            .SelectMany(bt => bt.TonKhos)
            .Sum(tk => tk.SoLuongTon);

        var model = new ProductViewModel
        {
            Id = p.Id,
            Name = p.TenSanPham,
            Category = p.DanhMuc?.TenDanhMuc ?? "Khác",
            Price = p.GiaBan,
            Stock = totalStock,
            Status = totalStock == 0
                ? "Hết hàng"
                : totalStock <= 10
                    ? "Sắp hết"
                    : "Đang bán",
            Image = p.SanPhamAnhs.FirstOrDefault()?.Url
                ?? "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=600"
        };

        ViewBag.Variants = p.BienTheSanPhams
            .Select(bt => new
            {
                bt.Id,
                bt.Sku,
                Size = bt.KichThuoc.MaSize,
                Color = bt.MauSac.TenMau,
                bt.MauSac.MaHex,
                bt.GiaBan,
                bt.TrangThai,
                Stock = bt.TonKhos.Sum(tk => tk.SoLuongTon)
            })
            .ToList();

        return View(model);
    }

    // 2. QUẢN LÝ ĐƠN ĐẶT HÀNG THẬT (Admin & Nhân viên)
    public async Task<IActionResult> Orders()
    {
        var orders = await _context.DonHangs
            .OrderByDescending(d => d.ThoiGianTao)
            .ToListAsync();

        ViewBag.PendingCount = orders.Count(o => o.TrangThai == "Cho_xac_nhan" || o.TrangThai == "Da_xac_nhan");
        ViewBag.ShippingCount = orders.Count(o => o.TrangThai == "Dang_giao");
        ViewBag.CompletedCount = orders.Count(o => o.TrangThai == "Da_giao");
        ViewBag.TotalMonthCount = orders.Count;

        return View(orders);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrderStatus(long id, string status)
    {
        var newStatus = status switch
        {
            "Chờ xử lý" => "Cho_xac_nhan",
            "Đang xử lý" or "Đã xác nhận" => "Da_xac_nhan",
            "Đang giao" => "Dang_giao",
            "Hoàn tất" => "Da_giao",
            "Đã hủy" => "Da_huy",
            _ => null
        };
        if (newStatus == null)
            return BadRequest("Trạng thái đơn hàng không hợp lệ.");

        var userId = long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedUserId)
            ? parsedUserId
            : (long?)null;
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"EXEC dbo.sp_CapNhatTrangThaiDon @DonHangID={id}, @TrangThaiMoi={newStatus}, @NguoiCapNhat={userId}, @GhiChu={status}");

        return RedirectToAction(nameof(Orders));
    }

    // 3. DANH MỤC SẢN PHẨM
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Categories()
    {
        var list = await _context.DanhMucs
            .Include(c => c.SanPhams)
            .OrderBy(c => c.Id)
            .ToListAsync();
        return View(list);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(string tenDanhMuc, string? moTa)
    {
        if (!string.IsNullOrWhiteSpace(tenDanhMuc))
        {
            var exists = await _context.DanhMucs.AnyAsync(c => c.TenDanhMuc == tenDanhMuc.Trim());
            if (!exists)
            {
                _context.DanhMucs.Add(new DanhMuc
                {
                    TenDanhMuc = tenDanhMuc.Trim(),
                    MoTa = moTa?.Trim(),
                    TrangThai = true,
                    ThoiGianTao = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }
        }
        return RedirectToAction(nameof(Categories));
    }

    // 4. KHO HÀNG & NHẬP KHO
    public async Task<IActionResult> Inventory()
    {
        var tonKhos = await _context.TonKhos
            .Include(t => t.Kho)
            .Include(t => t.BienThe)
                .ThenInclude(bt => bt.SanPham)
            .OrderBy(t => t.SoLuongTon)
            .ToListAsync();

        ViewBag.Khos = await _context.Khos.Where(k => k.TrangThai).ToListAsync();
        ViewBag.BienThes = await _context.BienTheSanPhams.Include(bt => bt.SanPham).ToListAsync();
        return View(tonKhos);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportStock(int khoId, long bienTheId, int soLuong)
    {
        if (soLuong > 0)
        {
            var tk = await _context.TonKhos.FirstOrDefaultAsync(t => t.KhoId == khoId && t.BienTheId == bienTheId);
            if (tk != null)
            {
                tk.SoLuongTon += soLuong;
            }
            else
            {
                _context.TonKhos.Add(new TonKho
                {
                    KhoId = khoId,
                    BienTheId = bienTheId,
                    SoLuongTon = soLuong,
                    SoLuongGiuCho = 0
                });
            }
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Inventory));
    }

    // 5. NGƯỜI DÙNG
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Users()
    {
        var users = await _context.NguoiDungs
            .Include(u => u.VaiTros)
            .OrderByDescending(u => u.Id)
            .ToListAsync();
        return View(users);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateUserStatus(long id, string trangThai)
    {
        var user = await _context.NguoiDungs.FindAsync(id);
        if (user != null && (trangThai == "Hoat_dong" || trangThai == "Khoa"))
        {
            user.TrangThai = trangThai;
            user.ThoiGianCapNhat = DateTime.Now;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Users));
    }

    // 6. NHÂN VIÊN
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Employees()
    {
        var list = await _context.NhanViens
            .Include(e => e.NguoiDung)
            .OrderByDescending(e => e.Id)
            .ToListAsync();
        return View(list);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddEmployee(string hoTen, string email, string? soDienThoai, string chucVu, decimal luong)
    {
        var cleanEmail = email.Trim();
        var user = await _context.NguoiDungs.FirstOrDefaultAsync(u => u.Email == cleanEmail);
        if (user == null)
        {
            var roleNv = await _context.VaiTros.FirstOrDefaultAsync(r => r.MaVaiTro == "NHAN_VIEN");
            user = new NguoiDung
            {
                TenDangNhap = cleanEmail,
                Email = cleanEmail,
                EmailChuan = cleanEmail.ToUpperInvariant(),
                MatKhauHash = "Nhanvien123@",
                HoTen = hoTen.Trim(),
                SoDienThoai = soDienThoai?.Trim(),
                TrangThai = "Hoat_dong",
                DaXacThucEmail = true,
                ThoiGianTao = DateTime.Now,
                ThoiGianCapNhat = DateTime.Now
            };
            if (roleNv != null) user.VaiTros.Add(roleNv);
            _context.NguoiDungs.Add(user);
            await _context.SaveChangesAsync();
        }

        var hasEmp = await _context.NhanViens.AnyAsync(e => e.NguoiDungId == user.Id);
        if (!hasEmp)
        {
            _context.NhanViens.Add(new NhanVien
            {
                NguoiDungId = user.Id,
                MaNhanVien = "NV" + DateTime.Now.ToString("mmss"),
                ChucVu = chucVu.Trim(),
                NgayVaoLam = DateOnly.FromDateTime(DateTime.Now),
                Luong = Math.Max(0, luong),
                TrangThai = "Dang_lam"
            });
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Employees));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateEmployeeStatus(long id, string trangThai)
    {
        var emp = await _context.NhanViens.FindAsync(id);
        if (emp != null && (trangThai == "Dang_lam" || trangThai == "Tam_nghi" || trangThai == "Da_nghi"))
        {
            emp.TrangThai = trangThai;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Employees));
    }

    // 7. KHUYẾN MÃI
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Promotions()
    {
        var promos = await _context.MaGiamGia
            .OrderByDescending(m => m.Id)
            .ToListAsync();
        return View(promos);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPromotion(string maVoucher, string moTa, string loaiGiam, decimal giaTri, int soLuongPhatHanh)
    {
        var code = maVoucher.Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(code) && !await _context.MaGiamGia.AnyAsync(m => m.MaVoucher == code))
        {
            _context.MaGiamGia.Add(new MaGiamGium
            {
                MaVoucher = code,
                MoTa = moTa.Trim(),
                LoaiGiam = loaiGiam == "Tien_mat" ? "Tien_mat" : "Phan_tram",
                GiaTri = loaiGiam == "Phan_tram" ? Math.Clamp(giaTri, 1, 100) : Math.Max(1000, giaTri),
                DonToiThieu = 0,
                SoLuongPhatHanh = Math.Max(1, soLuongPhatHanh),
                SoLuongDaDung = 0,
                NgayBatDau = DateTime.Now,
                NgayKetThuc = DateTime.Now.AddDays(30),
                TrangThai = true
            });
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Promotions));
    }

    // 8. ĐÁNH GIÁ SẢN PHẨM
    public async Task<IActionResult> Reviews()
    {
        var reviews = await _context.DanhGia
            .Include(r => r.SanPham)
            .Include(r => r.NguoiDung)
            .OrderByDescending(r => r.ThoiGianTao)
            .ToListAsync();
        return View(reviews);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateReviewStatus(long id, string trangThai)
    {
        var rv = await _context.DanhGia.FindAsync(id);
        if (rv != null && (trangThai == "Cho_duyet" || trangThai == "Da_duyet" || trangThai == "An"))
        {
            rv.TrangThai = trangThai;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Reviews));
    }

    // 9. BÁO CÁO THỐNG KÊ
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reports()
    {
        var validOrders = await _context.DonHangs
            .Where(d => d.TrangThai != "Da_huy")
            .ToListAsync();

        var totalRevenue = validOrders.Sum(d => d.TongThanhToan);
        var orderCount = validOrders.Count;
        var customerCount = await _context.NguoiDungs.CountAsync();

        ViewBag.DoanhThuThang = totalRevenue;
        ViewBag.TongDonHang = orderCount;
        ViewBag.TongKhachHang = customerCount;
        ViewBag.GiaTriDonTb = orderCount > 0 ? totalRevenue / orderCount : 0M;

        var year = DateTime.Now.Year;
        var monthlyTotals = Enumerable.Range(1, 12)
            .Select(m => validOrders.Where(d => d.ThoiGianTao.Year == year && d.ThoiGianTao.Month == m).Sum(d => d.TongThanhToan))
            .ToList();
        var maxMonthly = monthlyTotals.Max();
        ViewBag.MonthlyBars = monthlyTotals
            .Select(v => maxMonthly > 0 ? Math.Max(12, (int)Math.Round(v * 100M / maxMonthly)) : 15)
            .ToList();

        var topRaw = await _context.DonHangChiTiets
            .GroupBy(ct => ct.TenSanPham)
            .Select(g => new { Name = g.Key, Sold = g.Sum(x => x.SoLuong) })
            .OrderByDescending(x => x.Sold)
            .Take(4)
            .ToListAsync();

        var maxSold = topRaw.FirstOrDefault()?.Sold ?? 1;
        ViewBag.TopProducts = topRaw
            .Select(x => (x.Name, x.Sold, Math.Max(15, (int)Math.Round(x.Sold * 100.0 / maxSold))))
            .ToList();

        return View();
    }
}
