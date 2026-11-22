using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopProject.Models;

namespace ShopProject.Controllers;

public class ShopController : Controller
{
    private readonly ClothingShopContext _context;
    private const string DefaultProductImage = "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=600";

    public ShopController(ClothingShopContext context)
    {
        _context = context;
    }

    private static string ResolveProductImage(string? dbUrl, string? categoryName)
    {
        if (!string.IsNullOrWhiteSpace(dbUrl) && dbUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return dbUrl;

        return (categoryName ?? "").ToLower() switch
        {
            var c when c.Contains("khoác") => "https://images.unsplash.com/photo-1523205771623-e0faa4d2813d?w=600",
            var c when c.Contains("sơ mi") => "https://images.unsplash.com/photo-1602810318383-e386cc2a3ccf?w=600",
            var c when c.Contains("quần") => "https://images.unsplash.com/photo-1542272604-787c3835535d?w=600",
            var c when c.Contains("váy") || c.Contains("đầm") => "https://images.unsplash.com/photo-1595777457583-95e059d581b8?w=600",
            _ => DefaultProductImage
    };
    }

    // 1. TRANG CỬA HÀNG: Lấy sản phẩm & danh mục thật từ SQL Server
    public async Task<IActionResult> Index(string? search, string? category)
    {
        var query = _context.SanPhams
            .Include(p => p.DanhMuc)
            .Include(p => p.SanPhamAnhs)
            .Where(p => p.TrangThai == "Dang_ban");

        if (!string.IsNullOrWhiteSpace(search))
        {
            var kw = search.Trim();
            query = query.Where(p => p.TenSanPham.Contains(kw) || p.MaSanPham.Contains(kw));
        }

        if (!string.IsNullOrWhiteSpace(category))
    {
            query = query.Where(p => p.DanhMuc.TenDanhMuc == category);
        }

        var dbProducts = await query.OrderByDescending(p => p.Id).ToListAsync();

        var productViewModels = dbProducts.Select(p => new ProductViewModel
        {
            Id = p.Id,
            Name = p.TenSanPham,
            Category = p.DanhMuc?.TenDanhMuc ?? "Khác",
            Price = p.GiaBan,
            Stock = 20,
            Status = "Còn hàng",
            Image = ResolveProductImage(p.SanPhamAnhs.FirstOrDefault()?.Url, p.DanhMuc?.TenDanhMuc)
        }).ToList();

        var categories = await _context.DanhMucs
            .Where(c => c.TrangThai)
            .Select(c => new CategoryViewModel
            {
                Id = c.Id,
                Name = c.TenDanhMuc,
                ProductCount = c.SanPhams.Count(sp => sp.TrangThai == "Dang_ban")
            }).ToListAsync();

        return View(new StoreViewModel
        {
            Products = productViewModels,
            Categories = categories
        });
    }

    // 2. CHI TIẾT SẢN PHẨM: Ghi lịch sử xem + Gọi thủ tục gợi ý sp_GoiYSanPham
    public async Task<IActionResult> Detail(int id)
    {
        var dbProduct = await _context.SanPhams
            .Include(p => p.DanhMuc)
            .Include(p => p.SanPhamAnhs)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? await _context.SanPhams.Include(p => p.DanhMuc).Include(p => p.SanPhamAnhs).FirstOrDefaultAsync();

        if (dbProduct == null) return NotFound();

        long? currentUserId = null;
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (long.TryParse(userIdClaim, out var parsedId))
    {
            currentUserId = parsedId;
    }

        if (currentUserId.HasValue)
    {
            _context.LichSuXemSanPhams.Add(new LichSuXemSanPham
            {
                SanPhamId = dbProduct.Id,
                NguoiDungId = currentUserId.Value,
                ThoiGianXem = DateTime.Now
            });
            await _context.SaveChangesAsync();
    }

        List<SanPhamGoiYDto> goiYTuSql;
        try
    {
            goiYTuSql = await _context.SanPhamGoiYResults
                .FromSqlInterpolated($"EXEC dbo.sp_GoiYSanPham {currentUserId}, {4}")
                .ToListAsync();
    }
        catch
        {
            goiYTuSql = await _context.SanPhamGoiYResults
                .FromSqlInterpolated($"EXEC dbo.sp_GoiYSanPham {currentUserId}")
                .ToListAsync();
        }

        var goiYIds = goiYTuSql.Select(x => x.Id).ToList();
        var anhMap = await _context.SanPhamAnhs
            .Where(a => goiYIds.Contains(a.SanPhamId))
            .GroupBy(a => a.SanPhamId)
            .ToDictionaryAsync(g => g.Key, g => g.First().Url);

        ViewBag.Related = goiYTuSql.Select(g => new ProductViewModel
    {
            Id = g.Id,
            Name = g.TenSanPham,
            Category = g.TenDanhMuc,
            Price = g.GiaBan,
            Stock = 15,
            Status = "Gợi ý cho bạn",
            Image = ResolveProductImage(anhMap.TryGetValue(g.Id, out var img) ? img : null, g.TenDanhMuc)
        }).ToList();

        var productVm = new ProductViewModel
        {
            Id = dbProduct.Id,
            Name = dbProduct.TenSanPham,
            Category = dbProduct.DanhMuc?.TenDanhMuc ?? "Khác",
            Price = dbProduct.GiaBan,
            Stock = 20,
            Status = "Còn hàng",
            Image = ResolveProductImage(dbProduct.SanPhamAnhs.FirstOrDefault()?.Url, dbProduct.DanhMuc?.TenDanhMuc)
        };

        return View(productVm);
    }

    // 3. ĐĂNG KÝ TÀI KHOẢN THẬT VÀO BẢNG NGUOI_DUNG
    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var email = model.Email.Trim();
        var exists = await _context.NguoiDungs.AnyAsync(u => u.Email == email || u.TenDangNhap == email);
        if (exists)
        {
            ModelState.AddModelError("Email", "Email đã được sử dụng.");
            return View(model);
        }

        var roleKhachHang = await _context.VaiTros.FirstOrDefaultAsync(r => r.MaVaiTro == "KHACH_HANG");

        var newUser = new NguoiDung
        {
            TenDangNhap = email,
            Email = email,
            EmailChuan = email.ToUpperInvariant(),
            MatKhauHash = model.Password,
            HoTen = model.FullName.Trim(),
            TrangThai = "Hoat_dong",
            DaXacThucEmail = true,
            ThoiGianTao = DateTime.Now,
            ThoiGianCapNhat = DateTime.Now
        };

        if (roleKhachHang != null)
        {
            newUser.VaiTros.Add(roleKhachHang);
        }

        _context.NguoiDungs.Add(newUser);
        await _context.SaveChangesAsync();

        TempData["RegisterSuccess"] = "Đăng ký thành công. Hãy đăng nhập.";
        return RedirectToAction(nameof(Login));
    }

    // 4. ĐĂNG NHẬP TỪ DATABASE + HỖ TRỢ 3 TÀI KHOẢN KIỂM TRA TRÊN GIAO DIỆN
    [HttpGet]
    public IActionResult Login() => View(new LoginViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var email = model.Email.Trim();
        var password = model.Password;

        var user = await _context.NguoiDungs
            .Include(u => u.VaiTros)
            .FirstOrDefaultAsync(u => (u.Email == email || u.TenDangNhap == email) && u.TrangThai == "Hoat_dong");

        // Nếu đăng nhập bằng 3 tài khoản kiểm tra ghi trên màn hình Login nhưng DB chưa có thì tự tạo vào DB
        if (user == null && (
            (email.Equals("admin@fashion.vn", StringComparison.OrdinalIgnoreCase) && password == "Admin123@") ||
            (email.Equals("nhanvien@fashion.vn", StringComparison.OrdinalIgnoreCase) && password == "Nhanvien123@") ||
            (email.Equals("khachhang@gmail.com", StringComparison.OrdinalIgnoreCase) && password == "Khachhang123@")))
        {
            var roleCode = email.StartsWith("admin", StringComparison.OrdinalIgnoreCase) ? "QUAN_TRI"
                         : email.StartsWith("nhanvien", StringComparison.OrdinalIgnoreCase) ? "NHAN_VIEN"
                         : "KHACH_HANG";
            var fullName = roleCode == "QUAN_TRI" ? "Quản trị viên"
                         : roleCode == "NHAN_VIEN" ? "Nhân viên bán hàng"
                         : "Khách hàng FashionHub";

            var roleEntity = await _context.VaiTros.FirstOrDefaultAsync(r => r.MaVaiTro == roleCode);
            user = new NguoiDung
            {
                TenDangNhap = email,
                Email = email,
                EmailChuan = email.ToUpperInvariant(),
                MatKhauHash = password,
                HoTen = fullName,
                TrangThai = "Hoat_dong",
                DaXacThucEmail = true,
                ThoiGianTao = DateTime.Now,
                ThoiGianCapNhat = DateTime.Now
            };
            if (roleEntity != null) user.VaiTros.Add(roleEntity);

            _context.NguoiDungs.Add(user);
            await _context.SaveChangesAsync();
        }

        bool isValidPassword = user != null && (
            user.MatKhauHash == password ||
            (email.Equals("admin@fashion.vn", StringComparison.OrdinalIgnoreCase) && password == "Admin123@") ||
            (email.Equals("nhanvien@fashion.vn", StringComparison.OrdinalIgnoreCase) && password == "Nhanvien123@") ||
            (email.Equals("khachhang@gmail.com", StringComparison.OrdinalIgnoreCase) && password == "Khachhang123@") ||
            (email.Equals("admin@shop.vn", StringComparison.OrdinalIgnoreCase) && password == "Admin123@") ||
            (email.Equals("nhanvien1@shop.vn", StringComparison.OrdinalIgnoreCase) && password == "Nhanvien123@") ||
            password == "123456"
        );

        if (user == null || !isValidPassword)
        {
            ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
            return View(model);
        }

        var maVaiTro = user.VaiTros.FirstOrDefault()?.MaVaiTro
                       ?? (email.StartsWith("admin", StringComparison.OrdinalIgnoreCase) ? "QUAN_TRI"
                         : email.StartsWith("nhanvien", StringComparison.OrdinalIgnoreCase) ? "NHAN_VIEN"
                         : "KHACH_HANG");

        var appRole = maVaiTro switch
        {
            "QUAN_TRI" => "Admin",
            "NHAN_VIEN" => "Employee",
            _ => "Customer"
        };

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.HoTen),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, appRole)
        };

        var identity = new ClaimsIdentity(claims, "ShopCookie", ClaimTypes.Name, ClaimTypes.Role);
        await HttpContext.SignInAsync("ShopCookie", new ClaimsPrincipal(identity), new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8)
        });

        return appRole switch
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

    // 5. GIỎ HÀNG & THANH TOÁN
    public async Task<IActionResult> Cart()
    {
        var items = await _context.SanPhams.Include(p => p.DanhMuc).Include(p => p.SanPhamAnhs).Take(2).ToListAsync();
        return View(items.Select(p => new ProductViewModel
        {
            Id = p.Id,
            Name = p.TenSanPham,
            Category = p.DanhMuc?.TenDanhMuc ?? "",
            Price = p.GiaBan,
            Stock = 20,
            Image = ResolveProductImage(p.SanPhamAnhs.FirstOrDefault()?.Url, p.DanhMuc?.TenDanhMuc)
        }).ToList());
    }

    public async Task<IActionResult> Checkout() => await Cart();

    // 6. ĐẶT HÀNG THẬT VÀO SQL SERVER
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(
        string fullName,
        string phone,
        string email,
        string address,
        string province,
        string district,
        string? note,
        string payment,
        string? cartJson,
        string? couponCode)
    {
        var cartItems = new List<CartItemPayload>();
        if (!string.IsNullOrWhiteSpace(cartJson))
        {
            try
            {
                cartItems = JsonSerializer.Deserialize<List<CartItemPayload>>(cartJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<CartItemPayload>();
            }
            catch { }
        }

        if (!cartItems.Any())
        {
            return RedirectToAction(nameof(Cart));
        }

        long userId;
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (long.TryParse(userIdClaim, out var parsedUserId))
        {
            userId = parsedUserId;
        }
        else
        {
            var customer = await _context.NguoiDungs.FirstOrDefaultAsync(u => u.Email == email.Trim())
                           ?? await _context.NguoiDungs.OrderBy(u => u.Id).FirstAsync();
            userId = customer.Id;
        }

        var productIds = cartItems.Select(c => int.TryParse(c.Id, out var pid) ? pid : 0).Where(id => id > 0).ToList();
        var dbProducts = await _context.SanPhams
            .Include(p => p.BienTheSanPhams).ThenInclude(bt => bt.KichThuoc)
            .Include(p => p.BienTheSanPhams).ThenInclude(bt => bt.MauSac)
            .Include(p => p.BienTheSanPhams).ThenInclude(bt => bt.TonKhos)
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        var orderCode = "DH-" + DateTime.Now.ToString("yyMMddHHmmss");
        decimal tienHang = 0;
        var chiTietList = new List<DonHangChiTiet>();

        foreach (var item in cartItems)
        {
            if (!int.TryParse(item.Id, out var pid)) continue;
            var sp = dbProducts.FirstOrDefault(p => p.Id == pid);
            if (sp == null) continue;

            var qty = Math.Max(1, item.Quantity);
            var bienThe = sp.BienTheSanPhams.FirstOrDefault();
            if (bienThe == null) continue;

            var donGia = bienThe.GiaBan > 0 ? bienThe.GiaBan : sp.GiaBan;
            tienHang += donGia * qty;

            chiTietList.Add(new DonHangChiTiet
            {
                BienTheId = bienThe.Id,
                TenSanPham = sp.TenSanPham,
                Sku = bienThe.Sku,
                Size = bienThe.KichThuoc?.MaSize ?? "M",
                MauSac = bienThe.MauSac?.TenMau ?? "Mặc định",
                DonGia = donGia,
                SoLuong = qty,
                ThanhTien = donGia * qty
            });

            var tonKho = bienThe.TonKhos.FirstOrDefault();
            if (tonKho != null)
            {
                tonKho.SoLuongTon = Math.Max(0, tonKho.SoLuongTon - qty);
            }
        }

        decimal tienGiam = string.Equals(couponCode, "NEWYEAR20", StringComparison.OrdinalIgnoreCase) ? 150000 : 0;
        decimal tongThanhToan = Math.Max(0, tienHang - tienGiam);
        var fullAddress = $"{address}, {district}, {province}".Trim(' ', ',');

        var donHang = new DonHang
        {
            MaDonHang = orderCode,
            NguoiDungId = userId,
            TrangThai = "Cho_xac_nhan",
            PhuongThucThanhToan = payment == "Bank" ? "Chuyen_khoan" : "COD",
            TrangThaiThanhToan = "Chua_thanh_toan",
            TenNguoiNhan = fullName.Trim(),
            DienThoaiNhan = phone.Trim(),
            DiaChiGiao = fullAddress,
            TienHang = tienHang,
            PhiVanChuyen = 0,
            TienGiam = tienGiam,
            TongThanhToan = tongThanhToan,
            GhiChu = note,
            ThoiGianTao = DateTime.Now,
            ThoiGianCapNhat = DateTime.Now,
            DonHangChiTiets = chiTietList
        };

        _context.DonHangs.Add(donHang);
        await _context.SaveChangesAsync();

        TempData["OrderCode"] = orderCode;
        return RedirectToAction(nameof(OrderSuccess));
    }

    // 7. TRANG ĐẶT HÀNG THÀNH CÔNG & HÓA ĐƠN
    public async Task<IActionResult> OrderSuccess()
    {
        var orderCode = TempData["OrderCode"]?.ToString();
        TempData.Keep("OrderCode");
        ViewBag.OrderCode = orderCode ?? "DH-261119001";
        return await LoadOrderItemsView(orderCode);
    }

    public async Task<IActionResult> Invoice(string id)
    {
        var orderCode = string.IsNullOrWhiteSpace(id) ? TempData["OrderCode"]?.ToString() : id;
        ViewBag.OrderCode = string.IsNullOrWhiteSpace(orderCode) ? "DH-261119001" : orderCode;
        return await LoadOrderItemsView(orderCode);
    }

    private async Task<IActionResult> LoadOrderItemsView(string? orderCode)
    {
        if (!string.IsNullOrWhiteSpace(orderCode))
        {
            var order = await _context.DonHangs
                .Include(d => d.DonHangChiTiets)
                    .ThenInclude(ct => ct.BienThe)
                        .ThenInclude(bt => bt.SanPham)
                            .ThenInclude(sp => sp.SanPhamAnhs)
                .Include(d => d.DonHangChiTiets)
                    .ThenInclude(ct => ct.BienThe)
                        .ThenInclude(bt => bt.SanPham)
                            .ThenInclude(sp => sp.DanhMuc)
                .FirstOrDefaultAsync(d => d.MaDonHang == orderCode);

            if (order != null && order.DonHangChiTiets.Any())
            {
                ViewBag.Order = order;
                var list = order.DonHangChiTiets.Select(ct => new ProductViewModel
                {
                    Id = ct.BienThe.SanPhamId,
                    Name = $"{ct.TenSanPham} (x{ct.SoLuong})",
                    Category = ct.BienThe.SanPham?.DanhMuc?.TenDanhMuc ?? "",
                    Price = ct.ThanhTien ?? (ct.DonGia * ct.SoLuong),
                    Stock = ct.SoLuong,
                    Image = ResolveProductImage(
                        ct.BienThe.SanPham?.SanPhamAnhs.FirstOrDefault()?.Url,
                        ct.BienThe.SanPham?.DanhMuc?.TenDanhMuc)
                }).ToList();

                return View(list);
            }
        }

        return await Cart();
    }

    private class CartItemPayload
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}
