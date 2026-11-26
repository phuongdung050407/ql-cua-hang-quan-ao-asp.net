using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ShopProject.Models;
using System.Data;
using System.Security.Claims;
using System.Text.Json;
using PdfColors = QuestPDF.Helpers.Colors;
using PdfDocument = QuestPDF.Fluent.Document;

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

    private IQueryable<SanPham> AvailableRecommendationProducts()
    {
        return _context.SanPhams
            .AsNoTracking()
            .AsSplitQuery()
            .Include(product => product.DanhMuc)
            .Include(product => product.SanPhamAnhs)
            .Include(product => product.BienTheSanPhams).ThenInclude(variant => variant.TonKhos).ThenInclude(stock => stock.Kho)
            .Include(product => product.BienTheSanPhams).ThenInclude(variant => variant.KichThuoc)
            .Include(product => product.BienTheSanPhams).ThenInclude(variant => variant.MauSac)
            .Where(product => product.TrangThai == "Dang_ban"
                && product.BienTheSanPhams.Any(variant => variant.TrangThai
                    && variant.TonKhos.Any(stock => stock.Kho.TrangThai
                        && stock.SoLuongTon > stock.SoLuongGiuCho)));
    }

    private async Task<List<ProductViewModel>> GetRecommendedProducts(
        long? userId,
        int? excludedProductId,
        int? fallbackCategoryId,
        int count)
    {
        var products = new List<SanPham>();
        var suggestedIds = new List<int>();

        if (userId.HasValue)
        {
            var suggestions = await _context.SanPhamGoiYResults
                .FromSqlInterpolated($"EXEC dbo.sp_GoiYSanPham {userId.Value}, {count}")
                .ToListAsync();
            suggestedIds = suggestions
                .Select(suggestion => suggestion.Id)
                .Where(id => id != excludedProductId)
                .Distinct()
                .ToList();
        }

        if (suggestedIds.Count > 0)
        {
            var suggestedProducts = await AvailableRecommendationProducts()
                .Where(product => suggestedIds.Contains(product.Id))
                .ToListAsync();
            products.AddRange(suggestedProducts
                .OrderBy(product => suggestedIds.IndexOf(product.Id))
                .Take(count));
        }

        var selectedIds = products.Select(product => product.Id).ToHashSet();
        if (excludedProductId.HasValue)
            selectedIds.Add(excludedProductId.Value);

        if (products.Count < count && fallbackCategoryId.HasValue)
        {
            var categoryFallback = await AvailableRecommendationProducts()
                .Where(product => product.DanhMucId == fallbackCategoryId.Value && !selectedIds.Contains(product.Id))
                .OrderByDescending(product => product.ThoiGianTao)
                .Take(count - products.Count)
                .ToListAsync();
            products.AddRange(categoryFallback);
            selectedIds.UnionWith(categoryFallback.Select(product => product.Id));
        }

        if (products.Count < count)
        {
            var generalFallback = await AvailableRecommendationProducts()
                .Where(product => !selectedIds.Contains(product.Id))
                .OrderByDescending(product => product.ThoiGianTao)
                .Take(count - products.Count)
                .ToListAsync();
            products.AddRange(generalFallback);
        }

        return products.Select(product =>
        {
            var variants = product.BienTheSanPhams.Where(variant => variant.TrangThai).OrderBy(variant => variant.Id).ToList();
            var selectedVariant = variants.FirstOrDefault(variant => variant.TonKhos
                .Any(stock => stock.Kho.TrangThai && stock.SoLuongTon > stock.SoLuongGiuCho));
            var availableStock = selectedVariant?.TonKhos
                .Where(stock => stock.Kho.TrangThai)
                .Sum(stock => Math.Max(0, stock.SoLuongTon - stock.SoLuongGiuCho)) ?? 0;
            var category = product.DanhMuc?.TenDanhMuc ?? "Khác";

            return new ProductViewModel
            {
                Id = product.Id,
                VariantId = selectedVariant?.Id,
                Name = product.TenSanPham,
                Category = category,
                VariantLabel = selectedVariant == null ? "" : $"{selectedVariant.KichThuoc.MaSize} / {selectedVariant.MauSac.TenMau}",
                Price = selectedVariant?.GiaBan ?? product.GiaBan,
                Stock = Math.Max(0, availableStock),
                Status = availableStock > 0 ? "Còn hàng" : "Tạm hết hàng",
                Image = ResolveProductImage(product.SanPhamAnhs.FirstOrDefault()?.Url, category)
            };
        }).ToList();
    }

    // 1. TRANG CỬA HÀNG: Lấy sản phẩm & danh mục thật từ SQL Server
    public async Task<IActionResult> Index(string? search, string? category)
    {
        var query = _context.SanPhams
            .Include(p => p.DanhMuc)
            .Include(p => p.SanPhamAnhs)
            .Include(p => p.BienTheSanPhams).ThenInclude(v => v.TonKhos).ThenInclude(stock => stock.Kho)
            .Include(p => p.BienTheSanPhams).ThenInclude(v => v.KichThuoc)
            .Include(p => p.BienTheSanPhams).ThenInclude(v => v.MauSac)
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

        var productViewModels = dbProducts.Select(p =>
        {
            var variants = p.BienTheSanPhams.Where(v => v.TrangThai).OrderBy(v => v.Id).ToList();
            var selectedVariant = variants.FirstOrDefault(v => v.TonKhos.Any(stock => stock.Kho.TrangThai && stock.SoLuongTon > stock.SoLuongGiuCho))
                ?? variants.FirstOrDefault();
            var stock = selectedVariant?.TonKhos
                .Where(item => item.Kho.TrangThai)
                .Sum(item => Math.Max(0, item.SoLuongTon - item.SoLuongGiuCho)) ?? 0;
            return new ProductViewModel
            {
                Id = p.Id,
                VariantId = selectedVariant?.Id,
                Name = p.TenSanPham,
                Category = p.DanhMuc?.TenDanhMuc ?? "Khác",
                VariantLabel = selectedVariant == null ? "" : $"{selectedVariant.KichThuoc.MaSize} / {selectedVariant.MauSac.TenMau}",
                Price = selectedVariant?.GiaBan ?? p.GiaBan,
                Stock = Math.Max(0, stock),
                Status = stock > 0 ? "Còn hàng" : "Tạm hết hàng",
                Image = ResolveProductImage(p.SanPhamAnhs.FirstOrDefault()?.Url, p.DanhMuc?.TenDanhMuc)
            };
        }).ToList();

        var categories = await _context.DanhMucs
            .Where(c => c.TrangThai)
            .Select(c => new CategoryViewModel
            {
                Id = c.Id,
                Name = c.TenDanhMuc,
                ProductCount = c.SanPhams.Count(sp => sp.TrangThai == "Dang_ban")
            }).ToListAsync();

        long? currentUserId = null;
        if (long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            currentUserId = userId;

        return View(new StoreViewModel
        {
            Products = productViewModels,
            Categories = categories,
            Recommendations = await GetRecommendedProducts(currentUserId, null, null, 4)
        });
    }

    public async Task<IActionResult> Recommendations()
    {
        long? currentUserId = null;
        if (long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            currentUserId = userId;

        var recommendations = await GetRecommendedProducts(currentUserId, null, null, 8);
        return View(recommendations);
    }

    // 2. CHI TIẾT SẢN PHẨM: Ghi lịch sử xem + Gọi thủ tục gợi ý sp_GoiYSanPham + Lấy đánh giá thật
    public async Task<IActionResult> Detail(int id)
    {
        var dbProduct = await _context.SanPhams
            .Include(p => p.DanhMuc)
            .Include(p => p.SanPhamAnhs)
            .Include(p => p.BienTheSanPhams).ThenInclude(v => v.TonKhos).ThenInclude(stock => stock.Kho)
            .Include(p => p.BienTheSanPhams).ThenInclude(v => v.KichThuoc)
            .Include(p => p.BienTheSanPhams).ThenInclude(v => v.MauSac)
            .FirstOrDefaultAsync(p => p.Id == id && p.TrangThai == "Dang_ban");

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

        ViewBag.Related = await GetRecommendedProducts(currentUserId, dbProduct.Id, dbProduct.DanhMucId, 4);

        ViewBag.Reviews = await _context.DanhGia
            .Include(r => r.NguoiDung)
            .Where(r => r.SanPhamId == dbProduct.Id && r.TrangThai == "Da_duyet")
            .OrderByDescending(r => r.ThoiGianTao)
            .ToListAsync();

        var availableVariants = dbProduct.BienTheSanPhams.Where(v => v.TrangThai).OrderBy(v => v.Id).ToList();
        var selectedVariant = availableVariants.FirstOrDefault(v => v.TonKhos.Any(stock => stock.Kho.TrangThai && stock.SoLuongTon > stock.SoLuongGiuCho))
            ?? availableVariants.FirstOrDefault();
        var productStock = selectedVariant?.TonKhos
            .Where(stock => stock.Kho.TrangThai)
            .Sum(stock => Math.Max(0, stock.SoLuongTon - stock.SoLuongGiuCho)) ?? 0;
        var productVm = new ProductViewModel
        {
            Id = dbProduct.Id,
            VariantId = selectedVariant?.Id,
            Name = dbProduct.TenSanPham,
            Category = dbProduct.DanhMuc?.TenDanhMuc ?? "Khác",
            VariantLabel = selectedVariant == null ? "" : $"{selectedVariant.KichThuoc.MaSize} / {selectedVariant.MauSac.TenMau}",
            Price = selectedVariant?.GiaBan ?? dbProduct.GiaBan,
            Stock = Math.Max(0, productStock),
            Status = productStock > 0 ? "Còn hàng" : "Tạm hết hàng",
            Image = ResolveProductImage(dbProduct.SanPhamAnhs.FirstOrDefault()?.Url, dbProduct.DanhMuc?.TenDanhMuc)
        };

        return View(productVm);
    }

    // 2B. NHẬN ĐÁNH GIÁ MỚI TỪ KHÁCH HÀNG
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

        var daDanhGia = await _context.DanhGia
            .AnyAsync(r => r.NguoiDungId == userId
                        && r.SanPhamId == sanPhamId
                        && r.DonHangId == donHangHopLe.Id);

        if (daDanhGia)
        {
            TempData["ReviewError"] = "Bạn đã gửi đánh giá cho sản phẩm này ở đơn hàng gần nhất rồi.";
            return RedirectToAction(nameof(Detail), new { id = sanPhamId });
        }

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
        var phone = model.Phone.Trim();
        model.FullName = model.FullName.Trim();
        model.Email = email;
        model.Phone = phone;
        if (string.IsNullOrWhiteSpace(model.FullName))
        {
            ModelState.AddModelError(nameof(model.FullName), "Vui lòng nhập họ và tên.");
            return View(model);
        }

        var emailExists = await _context.NguoiDungs
            .AnyAsync(u => u.EmailChuan == emailLower || u.TenDangNhap == email);
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
            HoTen = model.FullName,
            SoDienThoai = phone,
            TrangThai = "Hoat_dong",
            DaXacThucEmail = false,
            ThoiGianTao = now,
            ThoiGianCapNhat = now,
            GioHang = new GioHang { ThoiGianTao = now }
        };
        newUser.MatKhauHash = hasher.HashPassword(newUser, model.Password);
        newUser.VaiTros.Add(roleKhachHang);

        try
        {
            _context.NguoiDungs.Add(newUser);
            await _context.SaveChangesAsync();

            TempData["RegisterSuccess"] = "Đăng ký thành công. Hãy đăng nhập.";
            return RedirectToAction(nameof(Login));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "Không thể hoàn tất đăng ký. Vui lòng kiểm tra thông tin hoặc thử lại.");
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

        var loginInput = (model.Email ?? string.Empty).Trim();
        var emailChuan = loginInput.ToLowerInvariant();
        var password = (model.Password ?? string.Empty).Trim();

        var user = await _context.NguoiDungs
            .Include(u => u.VaiTros)
            .FirstOrDefaultAsync(u =>
                (u.Email != null && u.Email.ToLower() == emailChuan)
                || (u.EmailChuan != null && u.EmailChuan.ToLower() == emailChuan)
                || (u.TenDangNhap != null && u.TenDangNhap.ToLower() == emailChuan));

        if (user == null)
        {
            ModelState.AddModelError("", "Email/tên đăng nhập hoặc mật khẩu không đúng.");
            return View(model);
        }

        var status = (user.TrangThai ?? string.Empty).Trim();
        if (string.Equals(status, "Khoa", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Bi_khoa", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Ngung_hoat_dong", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("", "Tài khoản hiện đang bị khóa.");
            return View(model);
        }

        var hasher = new PasswordHasher<NguoiDung>();
        bool isValidPassword = false;
        var storedHash = (user.MatKhauHash ?? string.Empty).Trim();

        if (!string.IsNullOrEmpty(storedHash))
        {
            if (storedHash.StartsWith("AQAAAA"))
            {
                var verifyResult = hasher.VerifyHashedPassword(user, storedHash, password);
                isValidPassword = verifyResult != PasswordVerificationResult.Failed;
            }
            else if (storedHash.StartsWith("$2a$") || storedHash.StartsWith("$2b$") || storedHash.StartsWith("$2y$"))
            {
                isValidPassword = BCrypt.Net.BCrypt.Verify(password, storedHash);
            }
            else
            {
                isValidPassword = storedHash == password || storedHash == (model.Password ?? string.Empty);
            }
        }

        if (!isValidPassword)
        {
            ModelState.AddModelError("", "Email/tên đăng nhập hoặc mật khẩu không đúng.");
            return View(model);
        }

        var nhanVien = await _context.NhanViens
            .AsNoTracking()
            .FirstOrDefaultAsync(nv => nv.NguoiDungId == user.Id);

        if (nhanVien != null && nhanVien.TrangThai == "Da_nghi")
        {
            ModelState.AddModelError("", "Tài khoản nhân viên đã nghỉ việc.");
            return View(model);
        }

        var now = DateTime.Now;
        user.ThoiGianCapNhat = now;
        if (string.IsNullOrWhiteSpace(user.TrangThai))
            user.TrangThai = "Hoat_dong";

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

        var maVaiTro = user.VaiTros.FirstOrDefault()?.MaVaiTro ?? "KHACH_HANG";
        var appRole = maVaiTro.ToUpperInvariant() switch
        {
            "ADMIN" or "QUAN_TRI" => "Admin",
            "NHAN_VIEN" or "QL_KHO" or "QL_SAN_PHAM" or "EMPLOYEE" => "Employee",
            _ => "Customer"
        };

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.HoTen ?? user.Email ?? "Người dùng"),
            new Claim(ClaimTypes.Email, user.Email ?? ""),
            new Claim("MaVaiTro", maVaiTro),
            new Claim(ClaimTypes.Role, appRole)
        };

        var identity = new ClaimsIdentity(claims, "ShopCookie", ClaimTypes.Name, ClaimTypes.Role);
        await HttpContext.SignInAsync("ShopCookie", new ClaimsPrincipal(identity), new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8)
        });

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return appRole switch
        {
            "Admin" => RedirectToAction("Index", "Home"),
            "Employee" => RedirectToAction("Orders", "Management"),
            _ => RedirectToAction(nameof(Index))
        };
    }

    // Đổi mật khẩu trương hợp quên mk
    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = model.Email.Trim().ToLower();

        var user = await _context.NguoiDungs
            .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == normalizedEmail);

        if (user == null)
        {
            ModelState.AddModelError(nameof(model.Email), "Không tìm thấy tài khoản với email này.");
            return View(model);
        }

        var hasher = new PasswordHasher<NguoiDung>();
        user.MatKhauHash = hasher.HashPassword(user, model.NewPassword);
        user.ThoiGianCapNhat = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập bằng mật khẩu mới.";
        return RedirectToAction("Login", "Shop");
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
    public IActionResult Cart()
    {
        return View(new List<ProductViewModel>());
    }

    [Authorize]
    public async Task<IActionResult> MyOrders()
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Challenge();

        var orders = await _context.DonHangs
            .AsNoTracking()
            .Where(order => order.NguoiDungId == userId)
            .Include(order => order.DonHangChiTiets)
            .OrderByDescending(order => order.ThoiGianTao)
            .ToListAsync();

        return View(orders);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> PersistentCart()
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Challenge();

        var cart = await _context.GioHangs
            .Include(item => item.GioHangChiTiets).ThenInclude(item => item.BienThe).ThenInclude(variant => variant.SanPham).ThenInclude(product => product.DanhMuc)
            .Include(item => item.GioHangChiTiets).ThenInclude(item => item.BienThe).ThenInclude(variant => variant.SanPham).ThenInclude(product => product.SanPhamAnhs)
            .Include(item => item.GioHangChiTiets).ThenInclude(item => item.BienThe).ThenInclude(variant => variant.KichThuoc)
            .Include(item => item.GioHangChiTiets).ThenInclude(item => item.BienThe).ThenInclude(variant => variant.MauSac)
            .Include(item => item.GioHangChiTiets).ThenInclude(item => item.BienThe).ThenInclude(variant => variant.TonKhos).ThenInclude(stock => stock.Kho)
            .FirstOrDefaultAsync(item => item.NguoiDungId == userId);

        var lines = cart?.GioHangChiTiets.Select(line =>
        {
            var variant = line.BienThe;
            var product = variant.SanPham;
            var availableStock = variant.TonKhos
                .Where(stock => stock.Kho.TrangThai)
                .Sum(stock => Math.Max(0, stock.SoLuongTon - stock.SoLuongGiuCho));
            var category = product.DanhMuc?.TenDanhMuc ?? "Khác";

            return (object)new
            {
                id = product.Id.ToString(),
                variantId = variant.Id,
                name = product.TenSanPham,
                category,
                image = ResolveProductImage(product.SanPhamAnhs.FirstOrDefault()?.Url, category),
                variantLabel = $"{variant.KichThuoc.MaSize} / {variant.MauSac.TenMau}",
                price = variant.GiaBan,
                stock = availableStock,
                quantity = line.SoLuong
            };
        }).ToList() ?? new List<object>();

        return Json(lines);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncCart(string? cartJson)
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(new { message = "Vui lòng đăng nhập lại." });

        List<CartItemPayload> items;
        try
        {
            items = JsonSerializer.Deserialize<List<CartItemPayload>>(cartJson ?? "[]", new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<CartItemPayload>();
        }
        catch (JsonException)
        {
            return BadRequest(new { message = "Không thể đọc nội dung giỏ hàng." });
        }

        if (items.Count > 100 || items.Any(item => !int.TryParse(item.Id, out var productId)
                || productId <= 0 || item.Quantity < 1 || item.Quantity > 99
                || (item.VariantId.HasValue && item.VariantId.Value < 1)))
            return BadRequest(new { message = "Sản phẩm hoặc số lượng trong giỏ không hợp lệ." });

        var normalizedItems = items
            .GroupBy(item => new { item.Id, item.VariantId })
            .Select(group => new CartItemPayload
            {
                Id = group.Key.Id,
                VariantId = group.Key.VariantId,
                Quantity = group.Sum(item => item.Quantity)
            })
            .ToList();
        if (normalizedItems.Any(item => item.Quantity > 99))
            return BadRequest(new { message = "Mỗi phiên bản chỉ được đặt tối đa 99 sản phẩm." });

        var productIds = normalizedItems.Select(item => int.Parse(item.Id)).Distinct().ToList();
        var products = await _context.SanPhams
            .Include(product => product.BienTheSanPhams).ThenInclude(variant => variant.TonKhos).ThenInclude(stock => stock.Kho)
            .Where(product => productIds.Contains(product.Id) && product.TrangThai == "Dang_ban")
            .ToListAsync();

        var selectedVariants = new List<(BienTheSanPham Variant, int Quantity)>();
        foreach (var item in normalizedItems)
        {
            var product = products.FirstOrDefault(candidate => candidate.Id == int.Parse(item.Id));
            var variants = product?.BienTheSanPhams.Where(variant => variant.TrangThai
                && (!item.VariantId.HasValue || variant.Id == item.VariantId.Value)).ToList();
            var variant = variants?.FirstOrDefault(candidate => candidate.TonKhos
                .Where(stock => stock.Kho.TrangThai)
                .Sum(stock => Math.Max(0, stock.SoLuongTon - stock.SoLuongGiuCho)) >= item.Quantity);
            if (variant == null)
                return BadRequest(new { message = "Một phiên bản đã hết hàng hoặc không đủ số lượng." });

            selectedVariants.Add((variant, item.Quantity));
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var cart = await _context.GioHangs
            .Include(item => item.GioHangChiTiets)
            .FirstOrDefaultAsync(item => item.NguoiDungId == userId);
        if (cart == null)
        {
            cart = new GioHang { NguoiDungId = userId, ThoiGianTao = DateTime.Now };
            _context.GioHangs.Add(cart);
        }
        else
        {
            _context.GioHangChiTiets.RemoveRange(cart.GioHangChiTiets);
            cart.GioHangChiTiets.Clear();
        }

        foreach (var item in selectedVariants)
        {
            cart.GioHangChiTiets.Add(new GioHangChiTiet
            {
                BienTheId = item.Variant.Id,
                SoLuong = item.Quantity,
                ThoiGianThem = DateTime.Now
            });
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return Json(new { success = true });
    }

    [Authorize]
    public IActionResult Checkout() => View(new List<ProductViewModel>());

    // 6. ĐẶT HÀNG THẬT VÀO SQL SERVER
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
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
        if (string.IsNullOrWhiteSpace(fullName)
            || string.IsNullOrWhiteSpace(phone)
            || string.IsNullOrWhiteSpace(address)
            || string.IsNullOrWhiteSpace(province)
            || string.IsNullOrWhiteSpace(district)
            || payment is not ("COD" or "Bank"))
        {
            TempData["CartError"] = "Vui lòng nhập đầy đủ thông tin giao hàng và phương thức thanh toán hợp lệ.";
            return RedirectToAction(nameof(Checkout));
        }

        var cartItems = new List<CartItemPayload>();
        try
        {
            cartItems = JsonSerializer.Deserialize<List<CartItemPayload>>(cartJson ?? "[]", new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<CartItemPayload>();
        }
        catch (JsonException)
        {
            TempData["CartError"] = "Không thể đọc giỏ hàng. Vui lòng kiểm tra lại sản phẩm.";
            return RedirectToAction(nameof(Cart));
        }

        if (cartItems.Any(item => !int.TryParse(item.Id, out var productId) || productId <= 0 || item.Quantity < 1
            || (item.VariantId.HasValue && item.VariantId.Value < 1)))
        {
            TempData["CartError"] = "Giỏ hàng chứa sản phẩm hoặc số lượng không hợp lệ.";
            return RedirectToAction(nameof(Cart));
        }

        var normalizedItems = cartItems
            .GroupBy(item => new { item.Id, item.VariantId })
            .Select(group => new CartItemPayload
            {
                Id = group.Key.Id,
                VariantId = group.Key.VariantId,
                Quantity = group.Sum(item => item.Quantity)
            })
            .ToList();

        if (normalizedItems.Count == 0 || normalizedItems.Any(item => item.Quantity > 99))
        {
            TempData["CartError"] = "Giỏ hàng trống hoặc số lượng sản phẩm không hợp lệ.";
            return RedirectToAction(nameof(Cart));
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdClaim, out var userId))
        {
            return Challenge();
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var productIds = normalizedItems.Select(item => int.Parse(item.Id)).ToList();
        var dbProducts = await _context.SanPhams
            .Include(p => p.BienTheSanPhams).ThenInclude(bt => bt.KichThuoc)
            .Include(p => p.BienTheSanPhams).ThenInclude(bt => bt.MauSac)
            .Include(p => p.BienTheSanPhams).ThenInclude(bt => bt.TonKhos).ThenInclude(stock => stock.Kho)
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        var orderCode = "DH-" + DateTime.Now.ToString("yyMMddHHmmss") + "-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        decimal tienHang = 0;
        var chiTietList = new List<DonHangChiTiet>();
        var reservations = new List<(DonHangChiTiet Detail, TonKho Stock, int Quantity)>();

        foreach (var item in normalizedItems)
        {
            var pid = int.Parse(item.Id);
            var sp = dbProducts.FirstOrDefault(p => p.Id == pid && p.TrangThai == "Dang_ban");
            var bienThe = sp?.BienTheSanPhams
                .Where(variant => variant.TrangThai
                    && (!item.VariantId.HasValue || variant.Id == item.VariantId.Value))
                .FirstOrDefault(variant => variant.TonKhos
                    .Where(stock => stock.Kho.TrangThai)
                    .Sum(stock => Math.Max(0, stock.SoLuongTon - stock.SoLuongGiuCho)) >= item.Quantity);
            if (sp == null || bienThe == null)
            {
                await transaction.RollbackAsync();
                TempData["CartError"] = "Một sản phẩm đã hết hàng hoặc không đủ số lượng. Vui lòng cập nhật giỏ hàng.";
                return RedirectToAction(nameof(Cart));
            }

            var donGia = bienThe.GiaBan > 0 ? bienThe.GiaBan : sp.GiaBan;
            tienHang += donGia * item.Quantity;

            var detail = new DonHangChiTiet
            {
                BienTheId = bienThe.Id,
                TenSanPham = sp.TenSanPham,
                Sku = bienThe.Sku,
                Size = bienThe.KichThuoc?.MaSize ?? "M",
                MauSac = bienThe.MauSac?.TenMau ?? "Mặc định",
                DonGia = donGia,
                SoLuong = item.Quantity
            };
            chiTietList.Add(detail);

            var remainingQuantity = item.Quantity;
            foreach (var tonKho in bienThe.TonKhos
                .Where(stock => stock.Kho.TrangThai)
                .OrderBy(stock => stock.KhoId))
            {
                var available = Math.Max(0, tonKho.SoLuongTon - tonKho.SoLuongGiuCho);
                var reservedFromWarehouse = Math.Min(available, remainingQuantity);
                if (reservedFromWarehouse > 0)
                    reservations.Add((detail, tonKho, reservedFromWarehouse));
                remainingQuantity -= reservedFromWarehouse;
                if (remainingQuantity == 0) break;
            }
        }

        decimal tienGiam = string.Equals(couponCode, "NEWYEAR20", StringComparison.OrdinalIgnoreCase)
            ? Math.Min(150000, tienHang)
            : 0;
        decimal tongThanhToan = Math.Max(0, tienHang - tienGiam);
        fullName = fullName.Trim();
        phone = phone.Trim();
        address = address.Trim();
        province = province.Trim();
        district = district.Trim();
        note = note?.Trim();
        var fullAddress = $"{address}, {district}, {province}".Trim(' ', ',');
        if (fullName.Length > 150 || phone.Length > 20 || fullAddress.Length > 500 || (note?.Length ?? 0) > 500)
        {
            await transaction.RollbackAsync();
            TempData["CartError"] = "Một số thông tin giao hàng vượt quá độ dài cho phép.";
            return RedirectToAction(nameof(Checkout));
        }

        var donHang = new DonHang
        {
            MaDonHang = orderCode,
            NguoiDungId = userId,
            TrangThai = "Cho_xac_nhan",
            PhuongThucThanhToan = payment == "Bank" ? "Chuyen_khoan" : "COD",
            TrangThaiThanhToan = "Chua_thanh_toan",
            TenNguoiNhan = fullName,
            DienThoaiNhan = phone,
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

        foreach (var reservation in reservations)
        {
            reservation.Stock.SoLuongGiuCho += reservation.Quantity;
            _context.ChiTietGiuChos.Add(new ChiTietGiuCho
            {
                DonHangCtId = reservation.Detail.Id,
                KhoId = reservation.Stock.KhoId,
                BienTheId = reservation.Detail.BienTheId,
                SoLuong = reservation.Quantity
            });
        }

        _context.ThanhToans.Add(new ThanhToan
        {
            DonHangId = donHang.Id,
            PhuongThuc = donHang.PhuongThucThanhToan,
            SoTien = tongThanhToan,
            TrangThai = "Khoi_tao",
            ThoiGianTao = DateTime.Now
        });
        _context.LichSuDonHangs.Add(new LichSuDonHang
        {
            DonHangId = donHang.Id,
            TrangThaiMoi = donHang.TrangThai,
            GhiChu = "Tạo đơn từ giỏ hàng",
            NguoiCapNhat = userId,
            ThoiGian = DateTime.Now
        });

        var userCart = await _context.GioHangs
            .Include(item => item.GioHangChiTiets)
            .FirstOrDefaultAsync(item => item.NguoiDungId == userId);
        if (userCart != null)
            _context.GioHangChiTiets.RemoveRange(userCart.GioHangChiTiets);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["OrderCode"] = orderCode;
        return RedirectToAction(nameof(OrderSuccess));
    }

    // 7. TRANG ĐẶT HÀNG THÀNH CÔNG & HÓA ĐƠN
    [Authorize]
    public async Task<IActionResult> OrderSuccess()
    {
        var orderCode = TempData["OrderCode"]?.ToString();
        TempData.Keep("OrderCode");
        ViewBag.OrderCode = orderCode ?? "DH-261119001";
        return await LoadOrderItemsView(orderCode);
    }

    [Authorize]
    public async Task<IActionResult> Invoice(string id)
    {
        var query = _context.DonHangs
            .Include(d => d.NguoiDung)
            .Include(d => d.DonHangChiTiets)
                .ThenInclude(ct => ct.BienThe)
                    .ThenInclude(bt => bt.SanPham)
                        .ThenInclude(sp => sp.SanPhamAnhs)
            .AsQueryable();

        var donHang = long.TryParse(id, out var orderId)
            ? await query.FirstOrDefaultAsync(d => d.MaDonHang == id || d.Id == orderId)
            : await query.FirstOrDefaultAsync(d => d.MaDonHang == id);

        ViewBag.OrderCode = donHang?.MaDonHang ?? id;
        ViewBag.Order = donHang;

        var items = donHang?.DonHangChiTiets.Select(ct => new ProductViewModel
        {
            Id = ct.BienThe.SanPhamId,
            Name = $"{ct.TenSanPham} ({ct.Size} - {ct.MauSac}) x{ct.SoLuong}",
            Price = ct.ThanhTien ?? (ct.DonGia * ct.SoLuong),
            Stock = ct.SoLuong,
            Image = ResolveProductImage(
                ct.BienThe.SanPham.SanPhamAnhs
                    .OrderByDescending(a => a.LaAnhDaiDien)
                    .Select(a => a.Url)
                    .FirstOrDefault(),
                null)
        }).ToList() ?? new List<ProductViewModel>();

        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExportInvoicePdf(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();

        var order = await _context.DonHangs
            .Include(d => d.DonHangChiTiets)
                .ThenInclude(ct => ct.BienThe)
                    .ThenInclude(bt => bt.KichThuoc)
            .Include(d => d.DonHangChiTiets)
                .ThenInclude(ct => ct.BienThe)
                    .ThenInclude(bt => bt.MauSac)
            .FirstOrDefaultAsync(d => d.MaDonHang == id);

        if (order == null) return NotFound();

        var paymentText = order.PhuongThucThanhToan switch
        {
            "COD" => "Thanh toán khi nhận hàng (COD)",
            "BANK" or "Chuyen_khoan" => "Chuyển khoản ngân hàng",
            _ => order.PhuongThucThanhToan ?? "Thanh toán khi nhận hàng (COD)"
        };

        var statusText = order.TrangThai switch
        {
            "Cho_xac_nhan" => "Chờ xác nhận",
            "Da_xac_nhan" => "Đã xác nhận",
            "Dang_giao" => "Đang giao hàng",
            "Da_giao" => "Hoàn tất",
            "Da_huy" => "Đã hủy",
            _ => order.TrangThai
        };

        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseSystemFonts = true;
        QuestPDF.Settings.ThrowOnMissingFontFamilies = false;

        var pdfBytes = PdfDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.PageColor(PdfColors.White);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10.5f).FontColor("#1E293B"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Row(brand =>
                        {
                            brand.ConstantItem(28).Height(28).Background("#18181B").AlignCenter().AlignMiddle()
                                .Text("F").FontColor(PdfColors.White).Bold().FontSize(13);
                            brand.RelativeItem().PaddingLeft(10).AlignMiddle()
                                .Text("FashionHub").FontSize(17).Bold().FontColor("#0F172A");
                        });

                        row.ConstantItem(230).AlignRight().Column(right =>
                        {
                            right.Item().Text("HÓA ĐƠN").FontSize(9).FontColor("#64748B").Bold();
                            right.Item().Text(order.MaDonHang).FontSize(12.5f).Bold().FontColor("#0F172A");
                        });
                    });

                    col.Item().PaddingTop(14).BorderBottom(1.5f).BorderColor("#18181B");
                });

                page.Content().PaddingVertical(18).Column(col =>
                {
                    col.Spacing(18);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text("KHÁCH HÀNG").FontSize(8.5f).Bold().FontColor("#94A3B8");
                            left.Item().PaddingTop(3).Text(order.TenNguoiNhan).FontSize(12).Bold().FontColor("#0F172A");
                            left.Item().PaddingTop(4).Text(order.DienThoaiNhan).FontSize(10).FontColor("#475569");
                            left.Item().PaddingTop(2).Text(order.DiaChiGiao).FontSize(10).FontColor("#475569");
                        });

                        row.RelativeItem().Column(right =>
                        {
                            right.Item().Text("THÔNG TIN ĐƠN").FontSize(8.5f).Bold().FontColor("#94A3B8");
                            right.Item().PaddingTop(4).Text($"Ngày lập: {order.ThoiGianTao:dd/MM/yyyy HH:mm}").FontSize(10).FontColor("#475569");
                            right.Item().PaddingTop(2).Text($"Phương thức: {paymentText}").FontSize(10).FontColor("#475569");
                            right.Item().PaddingTop(2).Text($"Trạng thái: {statusText}").FontSize(10).FontColor("#475569");
                        });
                    });

                    col.Item().PaddingTop(6).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3.5f);
                            columns.ConstantColumn(50);
                            columns.RelativeColumn(1.4f);
                            columns.RelativeColumn(1.5f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().BorderBottom(1).BorderColor("#E2E8F0").PaddingVertical(8).Text("Sản phẩm").Bold().FontSize(10);
                            header.Cell().BorderBottom(1).BorderColor("#E2E8F0").PaddingVertical(8).AlignCenter().Text("SL").Bold().FontSize(10);
                            header.Cell().BorderBottom(1).BorderColor("#E2E8F0").PaddingVertical(8).AlignRight().Text("Đơn giá").Bold().FontSize(10);
                            header.Cell().BorderBottom(1).BorderColor("#E2E8F0").PaddingVertical(8).AlignRight().Text("Thành tiền").Bold().FontSize(10);
                        });

                        foreach (var ct in order.DonHangChiTiets)
                        {
                            var size = ct.BienThe?.KichThuoc?.MaSize;
                            var color = ct.BienThe?.MauSac?.TenMau;
                            var variantLabel = (!string.IsNullOrEmpty(size) || !string.IsNullOrEmpty(color))
                                ? $"({size} - {color})"
                                : "";

                            table.Cell().BorderBottom(0.5f).BorderColor("#F1F5F9").PaddingVertical(9).Column(c =>
                            {
                                c.Item().Text(ct.TenSanPham).Bold().FontSize(10).FontColor("#0F172A");
                                if (!string.IsNullOrEmpty(variantLabel))
                                    c.Item().Text(variantLabel).FontSize(9).FontColor("#64748B");
                            });

                            table.Cell().BorderBottom(0.5f).BorderColor("#F1F5F9").PaddingVertical(9).AlignCenter().Text($"{ct.SoLuong}");
                            table.Cell().BorderBottom(0.5f).BorderColor("#F1F5F9").PaddingVertical(9).AlignRight().Text($"{ct.DonGia:N0} đ");
                            table.Cell().BorderBottom(0.5f).BorderColor("#F1F5F9").PaddingVertical(9).AlignRight().Text($"{(ct.DonGia * ct.SoLuong):N0} đ");
                        }
                    });

                    col.Item().AlignRight().Width(240).Column(sumCol =>
                    {
                        sumCol.Spacing(6);

                        void SumRow(string label, string val)
                        {
                            sumCol.Item().Row(r =>
                            {
                                r.RelativeItem().Text(label).FontSize(10).FontColor("#475569");
                                r.RelativeItem().AlignRight().Text(val).FontSize(10).Bold().FontColor("#0F172A");
                            });
                        }

                        SumRow("Tạm tính", $"{order.TienHang:N0} đ");
                        SumRow("Phí vận chuyển", $"{order.PhiVanChuyen:N0} đ");
                        SumRow("Giảm giá", order.TienGiam > 0 ? $"-{order.TienGiam:N0} đ" : "0 đ");

                        sumCol.Item().PaddingTop(4).BorderTop(1.5f).BorderColor("#18181B").PaddingTop(6).Row(r =>
                        {
                            r.RelativeItem().Text("Tổng cộng").FontSize(12).Bold().FontColor("#0F172A");
                            r.RelativeItem().AlignRight().Text($"{order.TongThanhToan:N0} đ").FontSize(13).Bold().FontColor("#0F172A");
                        });
                    });
                });

                page.Footer().AlignCenter().Text("Cảm ơn quý khách đã mua sắm tại FashionHub.")
                    .FontSize(9.5f).FontColor("#64748B");
            });
        }).GeneratePdf();

        var invoiceDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "invoices");
        Directory.CreateDirectory(invoiceDir);
        var fileName = $"HoaDon_{order.MaDonHang}.pdf";
        await System.IO.File.WriteAllBytesAsync(Path.Combine(invoiceDir, fileName), pdfBytes);

        return File(pdfBytes, "application/pdf", fileName);
    }

    private async Task<IActionResult> LoadOrderItemsView(string? orderCode)
    {
        if (!string.IsNullOrWhiteSpace(orderCode))
        {
            if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Challenge();

            var order = await _context.DonHangs
                .Where(d => d.MaDonHang == orderCode && d.NguoiDungId == userId)
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

        return Cart();
    }

    private class CartItemPayload
    {
        public string Id { get; set; } = "";
        public long? VariantId { get; set; }
        public int Quantity { get; set; }
    }
}