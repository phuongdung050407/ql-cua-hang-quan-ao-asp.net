using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopProject.Models;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Json;

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
            var cat = category.Trim();
            query = query.Where(p => p.DanhMuc != null && p.DanhMuc.TenDanhMuc.Contains(cat));
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

    // 2. CHI TIẾT SẢN PHẨM: Ghi lịch sử xem + Gọi thủ tục gợi ý sp_GoiYSanPham + Lấy đánh giá thật
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

        // Lấy danh sách đánh giá đã duyệt của sản phẩm từ bảng DanhGia
        ViewBag.Reviews = await _context.DanhGia
            .Include(r => r.NguoiDung)
            .Where(r => r.SanPhamId == dbProduct.Id && r.TrangThai == "Da_duyet")
            .OrderByDescending(r => r.ThoiGianTao)
            .ToListAsync();

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

    // 2B. NHẬN ĐÁNH GIÁ MỚI TỪ KHÁCH HÀNG (Khớp Trigger SQL: Chỉ đơn hàng trạng thái 'Da_giao' mới được đánh giá)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddReview(int sanPhamId, int soSao, string noiDung)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdClaim, out var userId))
        {
            TempData["ReviewError"] = "Vui lòng đăng nhập để gửi đánh giá sản phẩm.";
            return RedirectToAction(nameof(Login));
        }

        // 1. Kiểm tra khách đã có đơn hàng ĐÃ GIAO ('Da_giao') chứa sản phẩm này chưa
        var donHangHopLe = await _context.DonHangs
            .Include(d => d.DonHangChiTiets)
                .ThenInclude(ct => ct.BienThe)
            .Where(d => d.NguoiDungId == userId
                     && d.TrangThai == "Da_giao"
                     && d.DonHangChiTiets.Any(ct => ct.BienThe.SanPhamId == sanPhamId))
            .OrderByDescending(d => d.Id)
            .FirstOrDefaultAsync();

        if (donHangHopLe == null)
        {
            TempData["ReviewError"] = "Bạn chỉ có thể đánh giá sau khi đơn hàng chứa sản phẩm này đã được giao thành công!";
            return RedirectToAction(nameof(Detail), new { id = sanPhamId });
        }

        // 2. Kiểm tra chống spam đánh giá nhiều lần cho cùng một đơn hàng
        var daDanhGia = await _context.DanhGia
            .AnyAsync(r => r.NguoiDungId == userId
                        && r.SanPhamId == sanPhamId
                        && r.DonHangId == donHangHopLe.Id);

        if (daDanhGia)
        {
            TempData["ReviewError"] = "Bạn đã gửi đánh giá cho sản phẩm này ở đơn hàng gần nhất rồi.";
            return RedirectToAction(nameof(Detail), new { id = sanPhamId });
        }

        // 3. Lưu đánh giá hợp lệ vào CSDL
        var danhGia = new DanhGium
        {
            SanPhamId = sanPhamId,
            NguoiDungId = userId,
            DonHangId = donHangHopLe.Id,
            SoSao = (byte)Math.Clamp(soSao, 1, 5),
            NoiDung = string.IsNullOrWhiteSpace(noiDung) ? "Sản phẩm rất tốt!" : noiDung.Trim(),
            TrangThai = "Cho_duyet",
            ThoiGianTao = DateTime.Now
        };

        _context.DanhGia.Add(danhGia);
        await _context.SaveChangesAsync();

        TempData["ReviewSuccess"] = "Cảm ơn bạn đã đánh giá! Nhận xét của bạn đang chờ quản trị viên duyệt hiển thị.";
        return RedirectToAction(nameof(Detail), new { id = sanPhamId });
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
        var emailLower = email.ToLowerInvariant();

        var emailExists = await _context.NguoiDungs
            .AnyAsync(u => u.Email.ToLower() == emailLower || u.TenDangNhap == email);
        if (emailExists)
        {
            ModelState.AddModelError(nameof(model.Email), "Email hoặc tên đăng nhập đã được sử dụng.");
            return View(model);
        }

        var roleKhachHang = await _context.VaiTros
            .FirstOrDefaultAsync(r => r.MaVaiTro == "KHACH_HANG");
        if (roleKhachHang == null)
        {
            ModelState.AddModelError("", "Không tìm thấy vai trò KHACH_HANG trong hệ thống.");
            return View(model);
        }

        var now = DateTime.Now;
        var hasher = new PasswordHasher<NguoiDung>();

        var newUser = new NguoiDung
        {
            TenDangNhap = email,
            Email = email,
            HoTen = model.FullName.Trim(),
            TrangThai = "Hoat_dong",
            DaXacThucEmail = false,
            ThoiGianTao = now,
            ThoiGianCapNhat = now
        };
        newUser.MatKhauHash = hasher.HashPassword(newUser, model.Password);
        newUser.VaiTros.Add(roleKhachHang);

        try
        {
            _context.NguoiDungs.Add(newUser);
            await _context.SaveChangesAsync();

            _context.GioHangs.Add(new GioHang
            {
                NguoiDungId = newUser.Id,
                ThoiGianTao = now
            });
            await _context.SaveChangesAsync();

            TempData["RegisterSuccess"] = "Đăng ký thành công. Hãy đăng nhập.";
            return RedirectToAction(nameof(Login));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "Thông tin đăng ký bị trùng lặp hoặc không hợp lệ.");
            return View(model);
        }
    }

    // 4. ĐĂNG NHẬP TỪ DATABASE 
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
            return View(model);

        var loginInput = model.Email.Trim();
        var emailChuan = loginInput.ToLowerInvariant();
        var password = model.Password;

        // Tìm người dùng theo email_chuan hoặc ten_dang_nhap, trạng thái phải là Hoat_dong
        var user = await _context.NguoiDungs
            .Include(u => u.VaiTros)
            .FirstOrDefaultAsync(u =>
                (u.Email.ToLower() == emailChuan || u.TenDangNhap == loginInput)
                && u.TrangThai == "Hoat_dong");

        if (user == null)
        {
            ModelState.AddModelError("", "Email/tên đăng nhập hoặc mật khẩu không đúng.");
            return View(model);
        }

        var hasher = new PasswordHasher<NguoiDung>();
        bool isValidPassword = false;
        bool needsRehash = false;

        if (!string.IsNullOrEmpty(user.MatKhauHash) && user.MatKhauHash.StartsWith("AQAAAA"))
        {
            var verifyResult = hasher.VerifyHashedPassword(user, user.MatKhauHash, password);
            isValidPassword = verifyResult != PasswordVerificationResult.Failed;
            needsRehash = verifyResult == PasswordVerificationResult.SuccessRehashNeeded;
        }
        else
        {
            // Trường hợp mật khẩu chèn tay bằng SQL chưa được hash
            isValidPassword = user.MatKhauHash == password;
            needsRehash = isValidPassword;
        }

        if (!isValidPassword)
        {
            ModelState.AddModelError("", "Email/tên đăng nhập hoặc mật khẩu không đúng.");
            return View(model);
        }

        // Nếu tài khoản có bản ghi trong bảng nhan_vien, kiểm tra trạng thái nghỉ việc
        var nhanVien = await _context.NhanViens
            .AsNoTracking()
            .FirstOrDefaultAsync(nv => nv.NguoiDungId == user.Id);

        if (nhanVien != null && nhanVien.TrangThai == "Da_nghi")
        {
            ModelState.AddModelError("", "Tài khoản nhân viên đã nghỉ việc.");
            return View(model);
        }

        var now = DateTime.Now;

        // Cập nhật thoi_gian_cap_nhat theo quy ước SQL
        user.ThoiGianCapNhat = now;

        // Ghi nhật ký đăng nhập vào bảng nhat_ky_he_thong
        _context.NhatKyHeThongs.Add(new NhatKyHeThong
        {
            NguoiDungId = user.Id,
            HanhDong = "DANG_NHAP",
            DoiTuong = "nguoi_dung",
            DoiTuongId = user.Id,
            NoiDung = $"Đăng nhập hệ thống: {user.Email}",
            ThoiGian = now
        });

        await _context.SaveChangesAsync();

        // Ánh xạ ma_vai_tro từ bảng vai_tro trong SQL sang role ứng dụng
        var maVaiTro = user.VaiTros.FirstOrDefault()?.MaVaiTro ?? "KHACH_HANG";
        var appRole = maVaiTro switch
        {
            "ADMIN" => "Admin",
            "NHAN_VIEN" or "QL_KHO" or "QL_SAN_PHAM" => "Employee",
            _ => "Customer"
        };

        var claims = new[]
        {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.HoTen),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim("MaVaiTro", maVaiTro),
        new Claim(ClaimTypes.Role, appRole)
    };

        var identity = new ClaimsIdentity(claims, "ShopCookie", ClaimTypes.Name, ClaimTypes.Role);
        await HttpContext.SignInAsync("ShopCookie", new ClaimsPrincipal(identity), new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8)
        });

        // Kiểm tra và điều hướng theo vai trò
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return appRole switch
        {
            "Admin" => RedirectToAction("Index", "Home"),
            "Employee" => RedirectToAction("Orders", "Management"),
            _ => RedirectToAction(nameof(Index))
        };
    }

    // 5. ĐĂNG XUẤT
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