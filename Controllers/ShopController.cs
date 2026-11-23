using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.Data.SqlClient;
using ShopProject.Models.DTOs;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopProject.Models;
using ShopProject.Models.Entities;

namespace ShopProject.Controllers;

public class ShopController : Controller
{
    private readonly ClothingShopDbContext _context;

    public ShopController(ClothingShopDbContext context)
    {
        _context = context;
    }

    // --- TẠM GIỮ DỮ LIỆU MẪU SẢN PHẨM CHO ĐẾN BƯỚC 4.2 ---
    public async Task<IActionResult> Index(string? search, string? category)
    {
        // 1. Lấy danh sách danh mục đang hoạt động kèm số lượng sản phẩm
        var categories = await _context.DanhMucs
            .Where(dm => dm.TrangThai)
            .Select(dm => new CategoryViewModel
            {
                Id = dm.Id,
                Name = dm.TenDanhMuc,
                ProductCount = dm.SanPhams.Count(sp => sp.TrangThai == "Dang_ban"),
                Status = "Hoạt động"
            })
            .ToListAsync();

        // 2. Truy vấn sản phẩm đang bán từ SQL Server
        var query = _context.SanPhams
            .Include(sp => sp.DanhMuc)
            .Include(sp => sp.SanPhamAnhs)
            .Include(sp => sp.BienTheSanPhams)
                .ThenInclude(bt => bt.TonKhos)
            .Where(sp => sp.TrangThai == "Dang_ban");

        if (!string.IsNullOrWhiteSpace(search))
        {
            var kw = search.Trim();
            query = query.Where(sp => sp.TenSanPham.Contains(kw) || sp.MaSanPham.Contains(kw));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(sp => sp.DanhMuc.TenDanhMuc == category);
        }

        var sanPhams = await query
            .OrderByDescending(sp => sp.ThoiGianTao)
            .ToListAsync();

        var productViewModels = sanPhams.Select(MapToProductViewModel).ToList();

        // 3. Lấy danh sách Gợi ý sản phẩm qua Stored Procedure sp_GoiYSanPham
        var recommended = new List<ProductViewModel>();
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (long.TryParse(userIdStr, out var userId))
        {
            var goiYRaw = await _context.Database
                .SqlQueryRaw<GoiYSanPhamDto>("EXEC dbo.sp_GoiYSanPham @NguoiDungID = {0}, @SoLuong = {1}", userId, 4)
                .ToListAsync();

            if (goiYRaw.Count > 0)
            {
                var ids = goiYRaw.Select(g => g.Id).ToList();
                var anhMap = await _context.SanPhamAnhs
                    .Where(a => ids.Contains(a.SanPhamId))
                    .GroupBy(a => a.SanPhamId)
                    .ToDictionaryAsync(
                        g => g.Key,
                        g => g.OrderByDescending(x => x.LaAnhDaiDien).Select(x => x.Url).FirstOrDefault()
                    );

                recommended = goiYRaw.Select(g => new ProductViewModel
                {
                    Id = g.Id,
                    Name = g.TenSanPham,
                    Category = g.TenDanhMuc,
                    Price = g.GiaBan,
                    Status = "Còn hàng",
                    Image = anhMap.GetValueOrDefault(g.Id) ?? "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=600"
                }).ToList();
            }
        }

        // Nếu khách chưa đăng nhập hoặc chưa đủ 4 sản phẩm gợi ý thì lấy sản phẩm mới nhất
        if (recommended.Count == 0)
        {
            recommended = productViewModels.Take(4).ToList();
        }

        return View(new StoreViewModel
        {
            Products = productViewModels,
            Categories = categories,
            RecommendedProducts = recommended
        });
    }

    public async Task<IActionResult> Detail(int id)
    {
        
        // Trong truy vấn tìm sản phẩm:
        // .FirstOrDefaultAsync(sp => sp.Id == id && (isStaff || sp.TrangThai == "Dang_ban"));
        bool isStaff = User.IsInRole("Admin") || User.IsInRole("Employee");

        var sp = await _context.SanPhams
            .Include(s => s.DanhMuc)
            .Include(s => s.SanPhamAnhs)
            .Include(s => s.BienTheSanPhams)
                .ThenInclude(bt => bt.KichThuoc)
            .Include(s => s.BienTheSanPhams)
                .ThenInclude(bt => bt.MauSac)
            .Include(s => s.BienTheSanPhams)
                .ThenInclude(bt => bt.TonKhos)
            .FirstOrDefaultAsync(s => s.Id == id && (isStaff || s.TrangThai == "Dang_ban"));

        if (sp == null)
        {
            return RedirectToAction(nameof(Index));
        }
        // Lưu lịch sử xem sản phẩm nếu khách hàng đã đăng nhập (phục vụ sp_GoiYSanPham)
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (long.TryParse(userIdStr, out var userId))
        {
            _context.LichSuXemSanPhams.Add(new LichSuXemSanPham
            {
                NguoiDungId = userId,
                SanPhamId = sp.Id,
                ThoiGianXem = DateTime.Now
            });
            await _context.SaveChangesAsync();
        }

        var vm = MapToProductViewModel(sp);

        // Truyền thêm thông tin chi tiết sang View
        ViewBag.Description = sp.MoTa;
        ViewBag.Brand = sp.ThuongHieu?.TenThuongHieu;
        ViewBag.Material = sp.ChatLieu?.TenChatLieu;
        ViewBag.Images = sp.SanPhamAnhs
            .OrderByDescending(a => a.LaAnhDaiDien)
            .Select(a => a.Url)
            .DefaultIfEmpty(vm.Image)
            .ToList();

        ViewBag.Variants = sp.BienTheSanPhams
            .OrderBy(bt => bt.KichThuoc.ThuTu)
            .Select(bt => new
            {
                bt.Id,
                bt.Sku,
                Size = bt.KichThuoc.MaSize,
                Color = bt.MauSac.TenMau,
                ColorHex = bt.MauSac.MaHex ?? "#cccccc",
                Price = bt.GiaBan,
                Available = bt.TonKhos.Sum(tk => tk.SoLuongTon - tk.SoLuongGiuCho)
            })
            .ToList();

        ViewBag.Reviews = sp.DanhGia
            .OrderByDescending(dg => dg.ThoiGianTao)
            .ToList();

        var relatedEntities = await _context.SanPhams
            .Include(s => s.DanhMuc)
            .Include(s => s.SanPhamAnhs)
            .Include(s => s.BienTheSanPhams)
                .ThenInclude(bt => bt.TonKhos)
            .Where(s => s.TrangThai == "Dang_ban" && s.DanhMucId == sp.DanhMucId && s.Id != sp.Id)
            .Take(4)
            .ToListAsync();

        ViewBag.Related = relatedEntities.Select(MapToProductViewModel).ToList();

        return View(vm);
    }

    private static ProductViewModel MapToProductViewModel(SanPham sp)
    {
        var activeVariants = sp.BienTheSanPhams.Where(bt => bt.TrangThai).ToList();
        var minPrice = activeVariants.Count > 0 ? activeVariants.Min(bt => bt.GiaBan) : sp.GiaBan;
        var totalAvailable = activeVariants.Sum(bt => bt.TonKhos.Sum(tk => tk.SoLuongTon - tk.SoLuongGiuCho));
        var avatar = sp.SanPhamAnhs
            .OrderByDescending(a => a.LaAnhDaiDien)
            .Select(a => a.Url)
            .FirstOrDefault() ?? "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=600";

        return new ProductViewModel
        {
            Id = sp.Id,
            Name = sp.TenSanPham,
            Category = sp.DanhMuc?.TenDanhMuc ?? "Khác",
            Price = minPrice,
            Stock = totalAvailable,
            Status = totalAvailable > 0 ? "Còn hàng" : "Hết hàng",
            Image = avatar
        };
    }

// =========================================================================
    // GIỎ HÀNG & ĐẶT HÀNG (GỌI STORED PROCEDURE sp_TaoDonHangTuGioHang)
    // =========================================================================

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToCart(int? productId, long? variantId, int quantity = 1)
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return RedirectToAction(nameof(Login));

        quantity = Math.Max(1, quantity);

        // Tìm biến thể khách đã chọn (hoặc tự lấy biến thể đầu tiên còn hàng nếu bấm '+' ở trang chủ)
        BienTheSanPham? bienThe = null;
        if (variantId.HasValue && variantId.Value > 0)
        {
            bienThe = await _context.BienTheSanPhams
                .Include(bt => bt.TonKhos)
                .FirstOrDefaultAsync(bt => bt.Id == variantId.Value && bt.TrangThai);
        }
        else if (productId.HasValue && productId.Value > 0)
        {
            bienThe = await _context.BienTheSanPhams
                .Include(bt => bt.TonKhos)
                .Where(bt => bt.SanPhamId == productId.Value && bt.TrangThai)
                .OrderBy(bt => bt.Id)
                .FirstOrDefaultAsync();
        }

        if (bienThe == null)
        {
            TempData["CartError"] = "Sản phẩm hoặc phân loại không tồn tại.";
            return RedirectToAction(nameof(Index));
        }

        var available = bienThe.TonKhos.Sum(tk => tk.SoLuongTon - tk.SoLuongGiuCho);
        if (available < quantity)
        {
            TempData["CartError"] = "Phân loại này hiện không đủ số lượng trong kho.";
            return RedirectToAction(nameof(Detail), new { id = bienThe.SanPhamId });
        }

        // Tìm hoặc tạo giỏ hàng cho tài khoản
        var gioHang = await _context.GioHangs.FirstOrDefaultAsync(g => g.NguoiDungId == userId);
        if (gioHang == null)
        {
            gioHang = new GioHang
            {
                NguoiDungId = userId,
                ThoiGianTao = DateTime.Now
            };
            _context.GioHangs.Add(gioHang);
            await _context.SaveChangesAsync();
        }

        var chiTiet = await _context.GioHangChiTiets
            .FirstOrDefaultAsync(ct => ct.GioHangId == gioHang.Id && ct.BienTheId == bienThe.Id);

        if (chiTiet != null)
        {
            chiTiet.SoLuong = Math.Min(available, chiTiet.SoLuong + quantity);
            chiTiet.ThoiGianThem = DateTime.Now;
        }
        else
        {
            _context.GioHangChiTiets.Add(new GioHangChiTiet
            {
                GioHangId = gioHang.Id,
                BienTheId = bienThe.Id,
                SoLuong = quantity,
                ThoiGianThem = DateTime.Now
            });
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Cart));
    }

    [Authorize]
    public async Task<IActionResult> Cart()
    {
        var items = await GetUserCartItemsAsync();
        return View(items);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCart(long cartItemId, int delta)
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return RedirectToAction(nameof(Login));

        var item = await _context.GioHangChiTiets
            .Include(ct => ct.GioHang)
            .Include(ct => ct.BienThe)
                .ThenInclude(bt => bt.TonKhos)
            .FirstOrDefaultAsync(ct => ct.Id == cartItemId && ct.GioHang.NguoiDungId == userId);

        if (item != null)
        {
            var newQty = item.SoLuong + delta;
            var maxStock = item.BienThe.TonKhos.Sum(tk => tk.SoLuongTon - tk.SoLuongGiuCho);

            if (newQty <= 0)
            {
                _context.GioHangChiTiets.Remove(item);
            }
            else if (newQty <= maxStock)
            {
                item.SoLuong = newQty;
            }
            else
            {
                TempData["CartError"] = $"Trong kho chỉ còn tối đa {maxStock} sản phẩm cho phân loại này.";
            }

            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Cart));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveCart(long cartItemId)
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return RedirectToAction(nameof(Login));

        var item = await _context.GioHangChiTiets
            .Include(ct => ct.GioHang)
            .FirstOrDefaultAsync(ct => ct.Id == cartItemId && ct.GioHang.NguoiDungId == userId);

        if (item != null)
        {
            _context.GioHangChiTiets.Remove(item);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Cart));
    }

    [Authorize]
    public async Task<IActionResult> Checkout()
    {
        var items = await GetUserCartItemsAsync();
        if (items.Count == 0)
        {
            TempData["CartError"] = "Giỏ hàng của bạn đang trống.";
            return RedirectToAction(nameof(Cart));
        }

        if (long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            var user = await _context.NguoiDungs.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                ViewBag.FullName = user.HoTen;
                ViewBag.Phone = user.SoDienThoai ?? "";
                ViewBag.Email = user.Email;
                ViewBag.Address = "";
            }
        }

        // Giữ lại các trường khách đã nhập nếu lần đặt trước bị lỗi (không khôi phục maVoucher)
        if (TempData["FormFullName"] is string savedName) ViewBag.FullName = savedName;
        if (TempData["FormPhone"] is string savedPhone) ViewBag.Phone = savedPhone;
        if (TempData["FormEmail"] is string savedEmail) ViewBag.Email = savedEmail;
        if (TempData["FormAddress"] is string savedAddress) ViewBag.Address = savedAddress;
        ViewBag.Provinces = await _context.TinhThanhs
            .Where(t => t.TrangThai)
            .OrderBy(t => t.TenTinhThanh)
            .ToListAsync();
        ViewBag.District = TempData["FormDistrict"] as string ?? "";
        ViewBag.PaymentMethod = TempData["FormPayment"] as string ?? "COD";

        return View(items);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(
        string fullName,
        string phone,
        string address,
        string? province,
        string? district,
        string payment = "COD",
        string? maVoucher = null,
        bool daChuyenKhoan = false)
    {
        var tinhHopLe = await _context.TinhThanhs
            .AnyAsync(t => t.TenTinhThanh == province && t.TrangThai);

        if (!tinhHopLe)
        {
            TempData["FormFullName"] = fullName;
            TempData["FormPhone"] = phone;
            TempData["FormEmail"] = Request.Form["email"].ToString();
            TempData["FormAddress"] = address;
            TempData["FormDistrict"] = district;
            TempData["FormPayment"] = payment;
            TempData["CheckoutError"] = "Cửa hàng hiện chưa hỗ trợ hoặc đã tạm ngưng giao hàng tại khu vực bạn chọn.";
            return RedirectToAction(nameof(Checkout));
        }

        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return RedirectToAction(nameof(Login));

        var diaChiDayDu = string.Join(", ",
            new[] { address, district, province }.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!.Trim()));

        var phuongThucSql = payment switch
        {
            "Bank" or "Chuyen_khoan" => "Chuyen_khoan",
            "Wallet" or "Vi_dien_tu" => "Vi_dien_tu",
            _ => "COD"
        };

        var pUserId = new SqlParameter("@NguoiDungID", System.Data.SqlDbType.BigInt) { Value = userId };
        var pTen = new SqlParameter("@TenNguoiNhan", System.Data.SqlDbType.NVarChar, 150) { Value = (fullName ?? "").Trim() };
        var pSdt = new SqlParameter("@DienThoai", System.Data.SqlDbType.VarChar, 20) { Value = (phone ?? "").Trim() };
        var pDiaChi = new SqlParameter("@DiaChiGiao", System.Data.SqlDbType.NVarChar, 300) { Value = diaChiDayDu };
        var pPhuongThuc = new SqlParameter("@PhuongThuc", System.Data.SqlDbType.VarChar, 30) { Value = phuongThucSql };
        var pPhiShip = new SqlParameter("@PhiVanChuyen", System.Data.SqlDbType.Decimal)
        {
            Precision = 18,
            Scale = 2,
            Value = 0m
        };
        var pDonHangId = new SqlParameter("@DonHangID", System.Data.SqlDbType.BigInt)
        {
            Direction = System.Data.ParameterDirection.Output
        };
        var pVoucher = new SqlParameter("@MaVoucher", System.Data.SqlDbType.VarChar, 50)
        {
            Value = string.IsNullOrWhiteSpace(maVoucher) ? DBNull.Value : maVoucher.Trim()
        };

        try
        {
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_TaoDonHangTuGioHang @NguoiDungID, @TenNguoiNhan, @DienThoai, @DiaChiGiao, @PhuongThuc, @PhiVanChuyen, @DonHangID OUTPUT, @MaVoucher",
                pUserId, pTen, pSdt, pDiaChi, pPhuongThuc, pPhiShip, pDonHangId, pVoucher);

            var donHangId = Convert.ToInt64(pDonHangId.Value);
            var donHang = await _context.DonHangs.FindAsync(donHangId);

            if (donHang != null && phuongThucSql == "Chuyen_khoan")
            {
                if (daChuyenKhoan)
                {
                    // Nhánh 1: Đã ghi nhận chuyển khoản ngay lúc đặt
                    donHang.TrangThai = "Cho_xac_nhan";
                    TempData["PaymentStatusText"] = "Đã thanh toán chuyển khoản (Mô phỏng) • Đang chờ duyệt";
                }
                else
                {
                    // Nhánh 2: Chưa chuyển khoản -> Đang chờ thanh toán trong 24 giờ
                    donHang.TrangThai = "Cho_thanh_toan";
                    TempData["PaymentStatusText"] = "Đang chờ thanh toán (Hạn 24 giờ kể từ lúc đặt)";
                }
                await _context.SaveChangesAsync();
            }

            TempData["OrderCode"] = donHang?.MaDonHang ?? $"DH-{donHangId}";
            return RedirectToAction(nameof(OrderSuccess), new { id = donHangId });
        }
        catch (SqlException ex)
        {
            TempData["FormFullName"] = fullName;
            TempData["FormPhone"] = phone;
            TempData["FormEmail"] = Request.Form["email"].ToString();
            TempData["FormAddress"] = address;
            TempData["FormCity"] = Request.Form["province"].ToString();
            TempData["FormDistrict"] = district;
            TempData["FormPayment"] = Request.Form["payment"].ToString();

            if (ex.Message.Contains("giam gia", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("voucher", StringComparison.OrdinalIgnoreCase))
            {
                TempData["VoucherError"] = ex.Message.Contains("Ma giam gia khong hop le", StringComparison.OrdinalIgnoreCase)
                    ? "Mã giảm giá không hợp lệ, đã hết hạn hoặc đơn hàng chưa đủ điều kiện áp dụng."
                    : ex.Message;
            }
            else
            {
                TempData["CheckoutError"] = ex.Message;
            }

            return RedirectToAction(nameof(Checkout));
        }
    }
[Authorize]
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ConfirmTransferPaid(long orderId)
{
    var order = await _context.DonHangs.FirstOrDefaultAsync(d => d.Id == orderId);
    if (order == null) return NotFound();

    // Nếu đã quá 24 giờ kể từ lúc tạo đơn ở trạng thái chờ thanh toán -> tự hủy
    if (order.TrangThai == "Cho_thanh_toan" && DateTime.Now > order.ThoiGianTao.AddHours(24))
    {
        order.TrangThai = "Da_huy";
        await _context.SaveChangesAsync();
        TempData["InvoiceMessage"] = "Đơn hàng đã quá hạn 24 giờ chưa thanh toán nên tự động chuyển sang Đã hủy.";
        return RedirectToAction(nameof(Invoice), new { id = order.MaDonHang });
    }

    if (order.TrangThai == "Cho_thanh_toan")
    {
        order.TrangThai = "Cho_duyet"; // Hoặc "Cho_xac_nhan" tùy quy ước trạng thái của bạn
        await _context.SaveChangesAsync();
        TempData["InvoiceMessage"] = "Đã ghi nhận chuyển khoản thành công. Hóa đơn đã chuyển sang trạng thái Chờ duyệt.";
    }

    return RedirectToAction(nameof(Invoice), new { id = order.MaDonHang });
}

[Authorize]
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ExpireUnpaidTransferOrder(long orderId)
{
    var order = await _context.DonHangs.FirstOrDefaultAsync(d => d.Id == orderId);
    if (order != null && order.TrangThai == "Cho_thanh_toan" && DateTime.Now >= order.ThoiGianTao.AddHours(24))
    {
        order.TrangThai = "Da_huy";
        await _context.SaveChangesAsync();
    }
    return RedirectToAction(nameof(Index));
}
    [Authorize]
    public async Task<IActionResult> OrderSuccess(long? id)
    {
        var donHang = id.HasValue
            ? await _context.DonHangs
                .Include(d => d.DonHangChiTiets)
                    .ThenInclude(ct => ct.BienThe)
                        .ThenInclude(bt => bt.SanPham)
                            .ThenInclude(sp => sp.SanPhamAnhs)
                .FirstOrDefaultAsync(d => d.Id == id.Value)
            : null;

        ViewBag.OrderCode = donHang?.MaDonHang ?? TempData["OrderCode"]?.ToString() ?? "DH-0001";
        ViewBag.Order = donHang;

        var items = donHang?.DonHangChiTiets.Select(ct => new ProductViewModel
        {
            Id = ct.BienThe.SanPhamId,
            Name = $"{ct.TenSanPham} ({ct.Size} - {ct.MauSac}) x{ct.SoLuong}",
            Price = ct.ThanhTien ?? (ct.DonGia * ct.SoLuong),
            Stock = ct.SoLuong,
            Image = ct.BienThe.SanPham.SanPhamAnhs
                .OrderByDescending(a => a.LaAnhDaiDien)
                .Select(a => a.Url)
                .FirstOrDefault() ?? "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=600"
        }).ToList() ?? new List<ProductViewModel>();

        return View(items);
    }

    [Authorize]
    public async Task<IActionResult> Invoice(string id)
    {
        var donHang = await _context.DonHangs
            .Include(d => d.DonHangChiTiets)
                .ThenInclude(ct => ct.BienThe)
                    .ThenInclude(bt => bt.SanPham)
                        .ThenInclude(sp => sp.SanPhamAnhs)
            .FirstOrDefaultAsync(d => d.MaDonHang == id);

        ViewBag.OrderCode = donHang?.MaDonHang ?? id;
        ViewBag.Order = donHang;

        var items = donHang?.DonHangChiTiets.Select(ct => new ProductViewModel
        {
            Id = ct.BienThe.SanPhamId,
            Name = $"{ct.TenSanPham} ({ct.Size} - {ct.MauSac}) x{ct.SoLuong}",
            Price = ct.ThanhTien ?? (ct.DonGia * ct.SoLuong),
            Stock = ct.SoLuong,
            Image = ct.BienThe.SanPham.SanPhamAnhs
                .OrderByDescending(a => a.LaAnhDaiDien)
                .Select(a => a.Url)
                .FirstOrDefault() ?? "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=600"
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

        var pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10.5f).FontColor("#1E293B"));

                // Header hóa đơn
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Row(brand =>
                        {
                            brand.ConstantItem(28).Height(28).Background("#18181B").AlignCenter().AlignMiddle()
                                .Text("F").FontColor(Colors.White).Bold().FontSize(13);
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

                // Nội dung hóa đơn
                page.Content().PaddingVertical(18).Column(col =>
                {
                    col.Spacing(18);

                    // Thông tin khách hàng & thông tin đơn
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

                    // Bảng chi tiết sản phẩm
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

                    // Khối tổng tiền thanh toán
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

        // Lưu trữ bản sao PDF hóa đơn vào wwwroot/invoices/
        var invoiceDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "invoices");
        Directory.CreateDirectory(invoiceDir);
        var fileName = $"HoaDon_{order.MaDonHang}.pdf";
        await System.IO.File.WriteAllBytesAsync(Path.Combine(invoiceDir, fileName), pdfBytes);

        return File(pdfBytes, "application/pdf", fileName);
    }

    private async Task<List<CartItemViewModel>> GetUserCartItemsAsync()
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return [];

        var gioHang = await _context.GioHangs
            .Include(g => g.GioHangChiTiets)
                .ThenInclude(ct => ct.BienThe)
                    .ThenInclude(bt => bt.SanPham)
                        .ThenInclude(sp => sp.DanhMuc)
            .Include(g => g.GioHangChiTiets)
                .ThenInclude(ct => ct.BienThe)
                    .ThenInclude(bt => bt.SanPham)
                        .ThenInclude(sp => sp.SanPhamAnhs)
            .Include(g => g.GioHangChiTiets)
                .ThenInclude(ct => ct.BienThe)
                    .ThenInclude(bt => bt.KichThuoc)
            .Include(g => g.GioHangChiTiets)
                .ThenInclude(ct => ct.BienThe)
                    .ThenInclude(bt => bt.MauSac)
            .Include(g => g.GioHangChiTiets)
                .ThenInclude(ct => ct.BienThe)
                    .ThenInclude(bt => bt.TonKhos)
            .FirstOrDefaultAsync(g => g.NguoiDungId == userId);

        if (gioHang == null) return [];

        return gioHang.GioHangChiTiets.Select(ct => new CartItemViewModel
        {
            CartItemId = ct.Id,
            ProductId = ct.BienThe.SanPhamId,
            VariantId = ct.BienTheId,
            Name = ct.BienThe.SanPham.TenSanPham,
            Category = ct.BienThe.SanPham.DanhMuc?.TenDanhMuc ?? "",
            Size = ct.BienThe.KichThuoc.MaSize,
            Color = ct.BienThe.MauSac.TenMau,
            Price = ct.BienThe.GiaBan,
            Quantity = ct.SoLuong,
            MaxStock = ct.BienThe.TonKhos.Sum(tk => tk.SoLuongTon - tk.SoLuongGiuCho),
            Image = ct.BienThe.SanPham.SanPhamAnhs
                .OrderByDescending(a => a.LaAnhDaiDien)
                .Select(a => a.Url)
                .FirstOrDefault() ?? "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=600"
        }).ToList();
    }

    // =========================================================================
    // ĐĂNG KÝ, ĐĂNG NHẬP, ĐĂNG XUẤT KẾT NỐI SQL SERVER
    // =========================================================================

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var email = model.Email.Trim();
        var emailChuan = email.ToLowerInvariant();

        if (await _context.NguoiDungs.AnyAsync(u => u.EmailChuan == emailChuan))
        {
            ModelState.AddModelError("Email", "Email đã được sử dụng.");
            return View(model);
        }

        var nguoiDung = new NguoiDung
        {
            Email = email,
            HoTen = model.FullName.Trim(),
            SoDienThoai = model.Phone.Trim(),
            MatKhauHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            TrangThai = "Hoat_dong",
            ThoiGianTao = DateTime.Now,
            ThoiGianCapNhat = DateTime.Now
        };

        // Gán vai trò mặc định là KHACH_HANG
        var vaiTroKhach = await _context.VaiTros.FirstOrDefaultAsync(v => v.MaVaiTro == "KHACH_HANG");
        if (vaiTroKhach != null)
        {
            nguoiDung.VaiTros.Add(vaiTroKhach);
        }

        _context.NguoiDungs.Add(nguoiDung);
        await _context.SaveChangesAsync();

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

        var emailChuan = model.Email.Trim().ToLowerInvariant();
        var user = await _context.NguoiDungs
            .Include(u => u.VaiTros)
            .FirstOrDefaultAsync(u => u.EmailChuan == emailChuan);

        if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.MatKhauHash))
        {
            ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
            return View(model);
        }

        if (user.TrangThai != "Hoat_dong")
        {
            ModelState.AddModelError("", "Tài khoản của bạn đã bị khóa.");
            return View(model);
        }

        // Chuyển mã vai trò SQL sang tên Role giao diện đang sử dụng
        var maVaiTro = user.VaiTros.Select(v => v.MaVaiTro).FirstOrDefault() ?? "KHACH_HANG";
        var appRole = maVaiTro switch
        {
            "ADMIN" => "Admin",
            "NHAN_VIEN" or "QL_KHO" or "QL_SAN_PHAM" => "Employee",
            _ => "Customer"
        };

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.HoTen),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, appRole)
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
}