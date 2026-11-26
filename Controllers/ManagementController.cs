using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ShopProject.Models;

namespace ShopProject.Controllers;

[Authorize(Roles = "Admin,Employee")]
public class ManagementController : Controller
{
    private readonly ClothingShopContext _context;

    public ManagementController(ClothingShopContext context)
    {
        _context = context;
    }

    // =========================================================================
    // 1. QUẢN LÝ ĐƠN ĐẶT HÀNG (THEO origin/main ĐỂ KHỚP VIEW Orders.cshtml)
    // =========================================================================

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

    // =========================================================================
    // 2. QUẢN LÝ KHO HÀNG & PHIẾU NHẬP (PHẦN CỦA BẠN)
    // =========================================================================

    public async Task<IActionResult> Inventory(string? search, int? khoId, string? alertFilter)
    {
        var query = _context.TonKhos
            .Include(t => t.Kho)
            .Include(t => t.BienThe)
                .ThenInclude(bt => bt.SanPham)
            .Include(t => t.BienThe)
                .ThenInclude(bt => bt.KichThuoc)
            .Include(t => t.BienThe)
                .ThenInclude(bt => bt.MauSac)
            .AsQueryable();

        if (khoId.HasValue && khoId.Value > 0)
        {
            query = query.Where(t => t.KhoId == khoId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var kw = search.Trim().ToLower();
            query = query.Where(t =>
                t.BienThe.Sku.ToLower().Contains(kw) ||
                t.BienThe.SanPham.TenSanPham.ToLower().Contains(kw));
        }

        var tonKhos = await query
            .OrderBy(t => t.SoLuongTon - t.SoLuongGiuCho)
            .ThenBy(t => t.BienThe.Sku)
            .ToListAsync();

        var cartStats = await _context.GioHangChiTiets
            .GroupBy(g => g.BienTheId)
            .Select(g => new { BienTheId = g.Key, Qty = g.Sum(x => x.SoLuong) })
            .ToDictionaryAsync(x => x.BienTheId, x => x.Qty);

        var soldStats = await _context.DonHangChiTiets
            .Where(ct => ct.DonHang.TrangThai != "Da_huy")
            .GroupBy(ct => ct.BienTheId)
            .Select(g => new { BienTheId = g.Key, Qty = g.Sum(x => x.SoLuong) })
            .ToDictionaryAsync(x => x.BienTheId, x => x.Qty);

        var alertMap = new Dictionary<string, (string Level, string Tag, string Reason, int InCart, int Sold, int SuggestedQty)>();
        foreach (var tk in tonKhos)
        {
            var key = $"{tk.KhoId}_{tk.BienTheId}";
            var coTheBan = tk.SoLuongTon - tk.SoLuongGiuCho;
            var inCart = cartStats.GetValueOrDefault(tk.BienTheId, 0);
            var sold = soldStats.GetValueOrDefault(tk.BienTheId, 0);
            var demand = inCart + tk.SoLuongGiuCho + sold;

            if (coTheBan <= 0)
            {
                var suggest = Math.Max(15, demand + 10);
                alertMap[key] = ("out", "Hết hàng • Nhập gấp", $"Hết hàng khả dụng ({inCart} trong giỏ, {tk.SoLuongGiuCho} đang giữ)", inCart, sold, suggest);
            }
            else if (coTheBan <= 5)
            {
                var suggest = Math.Max(15, demand + 10 - coTheBan);
                alertMap[key] = ("low", "Sắp hết hàng", $"Chỉ còn {coTheBan} sp có thể bán", inCart, sold, suggest);
            }
            else if (coTheBan < inCart + tk.SoLuongGiuCho || inCart >= 3 || tk.SoLuongGiuCho >= 2 || sold >= 5)
            {
                var suggest = Math.Max(10, (inCart + tk.SoLuongGiuCho + 10) - coTheBan);
                alertMap[key] = ("demand", "Nhắc nhập kho • Nhu cầu cao", $"Có {inCart} trong giỏ, {tk.SoLuongGiuCho} giữ chỗ, {sold} đã bán", inCart, sold, suggest);
            }
            else
            {
                alertMap[key] = ("ok", "Ổn định", "Tồn kho an toàn", inCart, sold, 10);
            }
        }

        if (!string.IsNullOrWhiteSpace(alertFilter))
        {
            tonKhos = alertFilter switch
            {
                "warning" => tonKhos.Where(t => alertMap[$"{t.KhoId}_{t.BienTheId}"].Level != "ok").ToList(),
                "out" => tonKhos.Where(t => alertMap[$"{t.KhoId}_{t.BienTheId}"].Level == "out").ToList(),
                "low" => tonKhos.Where(t => alertMap[$"{t.KhoId}_{t.BienTheId}"].Level == "low").ToList(),
                "demand" => tonKhos.Where(t => alertMap[$"{t.KhoId}_{t.BienTheId}"].Level == "demand").ToList(),
                "ok" => tonKhos.Where(t => alertMap[$"{t.KhoId}_{t.BienTheId}"].Level == "ok").ToList(),
                _ => tonKhos
            };
        }

        ViewBag.Search = search;
        ViewBag.SelectedKho = khoId;
        ViewBag.AlertFilter = alertFilter ?? "";
        ViewBag.AlertMap = alertMap;
        ViewBag.Khos = await _context.Khos.Where(k => k.TrangThai).ToListAsync();
        ViewBag.AllVariants = await _context.BienTheSanPhams
            .Include(bt => bt.SanPham)
            .Include(bt => bt.KichThuoc)
            .Include(bt => bt.MauSac)
            .OrderBy(bt => bt.Sku)
            .ToListAsync();

        return View(tonKhos);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportStock(int khoId, long bienTheId, int soLuongNhap, string? nhaCungCap = null, string? ghiChu = null)
    {
        if (soLuongNhap <= 0)
        {
            TempData["InventoryError"] = "Số lượng nhập kho phải lớn hơn 0.";
            return RedirectToAction(nameof(Inventory));
        }

        var bt = await _context.BienTheSanPhams
            .Include(b => b.SanPham)
            .FirstOrDefaultAsync(b => b.Id == bienTheId);

        if (bt == null)
        {
            TempData["InventoryError"] = "Không tìm thấy phân loại sản phẩm cần nhập.";
            return RedirectToAction(nameof(Inventory));
        }

        var tk = await _context.TonKhos.FirstOrDefaultAsync(t => t.KhoId == khoId && t.BienTheId == bienTheId);
        if (tk != null)
        {
            tk.SoLuongTon += soLuongNhap;
        }
        else
        {
            _context.TonKhos.Add(new TonKho
            {
                KhoId = khoId,
                BienTheId = bienTheId,
                SoLuongTon = soLuongNhap,
                SoLuongGiuCho = 0
            });
        }

        long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
        var nhanVien = await _context.NhanViens.FirstOrDefaultAsync(nv => nv.NguoiDungId == userId)
                       ?? await _context.NhanViens.FirstOrDefaultAsync();

        if (nhanVien != null)
        {
            var phieu = new PhieuNhapKho
            {
                MaPhieu = "PN-" + DateTime.Now.ToString("yyMMddHHmmss"),
                KhoId = khoId,
                NhanVienId = nhanVien.Id,
                NhaCungCap = string.IsNullOrWhiteSpace(nhaCungCap) ? "Nhập bổ sung nội bộ" : nhaCungCap.Trim(),
                NgayNhap = DateTime.Now,
                GhiChu = string.IsNullOrWhiteSpace(ghiChu) ? $"Nhập nhanh mã {bt.Sku}" : ghiChu.Trim(),
                ChiTietPhieuNhaps = new List<ChiTietPhieuNhap>
                {
                    new ChiTietPhieuNhap
                    {
                        BienTheId = bienTheId,
                        SoLuong = soLuongNhap,
                        DonGia = bt.SanPham.GiaGoc > 0 ? bt.SanPham.GiaGoc : bt.GiaBan
                    }
                }
            };
            _context.PhieuNhapKhos.Add(phieu);
        }

        await _context.SaveChangesAsync();
        TempData["InventorySuccess"] = $"Đã nhập thêm +{soLuongNhap} sản phẩm ({bt.Sku}) và lưu phiếu nhập kho thành công.";
        return RedirectToAction(nameof(Inventory));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustStock(int khoId, long bienTheId, int soLuongMoi)
    {
        var tk = await _context.TonKhos
            .Include(t => t.BienThe)
            .FirstOrDefaultAsync(t => t.KhoId == khoId && t.BienTheId == bienTheId);

        if (tk == null) return RedirectToAction(nameof(Inventory));

        if (soLuongMoi < tk.SoLuongGiuCho)
        {
            TempData["InventoryError"] = $"Không thể điều chỉnh tồn kho ({soLuongMoi}) nhỏ hơn số lượng đang giữ chỗ ({tk.SoLuongGiuCho}).";
            return RedirectToAction(nameof(Inventory));
        }

        tk.SoLuongTon = soLuongMoi;
        await _context.SaveChangesAsync();
        TempData["InventorySuccess"] = $"Đã cập nhật tồn kho thực tế của mã {tk.BienThe.Sku} thành {soLuongMoi}.";
        return RedirectToAction(nameof(Inventory));
    }

    public async Task<IActionResult> DownloadInventoryTemplate()
    {
        var items = await _context.TonKhos
            .Include(t => t.Kho)
            .Include(t => t.BienThe)
                .ThenInclude(bt => bt.SanPham)
            .Include(t => t.BienThe)
                .ThenInclude(bt => bt.KichThuoc)
            .Include(t => t.BienThe)
                .ThenInclude(bt => bt.MauSac)
            .OrderBy(t => t.BienThe.Sku)
            .ToListAsync();

        var rows = new List<string[]>
        {
            new[] { "MaSKU", "TenSanPham", "MaKho", "TonHienTai", "SoLuongNhapThem" }
        };

        foreach (var t in items)
        {
            var name = $"{t.BienThe.SanPham.TenSanPham} ({t.BienThe.KichThuoc?.MaSize} - {t.BienThe.MauSac?.TenMau})";
            rows.Add(new[] { t.BienThe.Sku, name, t.Kho.MaKho, t.SoLuongTon.ToString(), "0" });
        }

        var bytes = BuildSimpleXlsx(rows);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Mau_NhapKho_FashionHub.xlsx");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> ImportStockExcel(IFormFile? excelFile)
    {
        const long maxFileBytes = 5 * 1024 * 1024;

        if (excelFile == null || excelFile.Length == 0)
        {
            TempData["InventoryError"] = "Vui lòng chọn file Excel (.xlsx hoặc .csv) trước khi xác nhận nhập kho.";
            return RedirectToAction(nameof(Inventory));
        }

        if (excelFile.Length > maxFileBytes)
        {
            TempData["InventoryError"] = $"File \"{excelFile.FileName}\" vượt quá dung lượng cho phép (tối đa 5 MB).";
            return RedirectToAction(nameof(Inventory));
        }

        var ext = Path.GetExtension(excelFile.FileName).ToLowerInvariant();
        if (ext != ".xlsx" && ext != ".csv")
        {
            TempData["InventoryError"] = "Định dạng không hợp lệ. Hệ thống chỉ hỗ trợ file Excel (.xlsx) hoặc (.csv).";
            return RedirectToAction(nameof(Inventory));
        }

        try
        {
            using var fileStream = excelFile.OpenReadStream();
            var tableRows = ext == ".csv" ? ParseCsvRows(fileStream) : ParseXlsxRows(fileStream);

            if (tableRows.Count <= 1)
            {
                TempData["InventoryError"] = "File Excel không có dữ liệu nhập kho.";
                return RedirectToAction(nameof(Inventory));
            }

            var defaultKho = await _context.Khos.FirstOrDefaultAsync(k => k.TrangThai);
            var khoDict = await _context.Khos.ToDictionaryAsync(k => k.MaKho.ToUpperInvariant(), k => k.Id);
            var variantDict = await _context.BienTheSanPhams.ToDictionaryAsync(b => b.Sku.ToUpperInvariant(), b => b.Id);

            int updatedCount = 0;
            int totalAdded = 0;

            foreach (var row in tableRows.Skip(1))
            {
                if (row.Length == 0 || string.IsNullOrWhiteSpace(row[0])) continue;

                var sku = row[0].Trim().ToUpperInvariant();
                if (!variantDict.TryGetValue(sku, out var bienTheId)) continue;

                var qtyStr = row.Length >= 5 ? row[4] : row[^1];
                if (!int.TryParse(qtyStr?.Trim(), out var qty) || qty <= 0) continue;

                int targetKhoId = defaultKho?.Id ?? 1;
                if (row.Length >= 3 && !string.IsNullOrWhiteSpace(row[2]) &&
                    khoDict.TryGetValue(row[2].Trim().ToUpperInvariant(), out var foundKhoId))
                {
                    targetKhoId = foundKhoId;
                }

                var tk = await _context.TonKhos.FirstOrDefaultAsync(t => t.KhoId == targetKhoId && t.BienTheId == bienTheId);
                if (tk != null)
                {
                    tk.SoLuongTon += qty;
                }
                else
                {
                    _context.TonKhos.Add(new TonKho
                    {
                        KhoId = targetKhoId,
                        BienTheId = bienTheId,
                        SoLuongTon = qty,
                        SoLuongGiuCho = 0
                    });
                }

                updatedCount++;
                totalAdded += qty;
            }

            await _context.SaveChangesAsync();

            if (updatedCount == 0)
            {
                TempData["InventoryError"] = "Không có dòng nào hợp lệ để nhập (kiểm tra lại cột MaSKU và cột SoLuongNhapThem > 0).";
            }
            else
            {
                TempData["InventorySuccess"] = $"Nhập kho từ file \"{excelFile.FileName}\" thành công: cập nhật {updatedCount} mã SKU (+{totalAdded} sản phẩm).";
            }
        }
        catch (Exception ex)
        {
            TempData["InventoryError"] = $"Lỗi đọc file Excel: {ex.Message}";
        }

        return RedirectToAction(nameof(Inventory));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadInventoryExcel(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { ok = false, message = "Vui lòng chọn file Excel hợp lệ." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".xlsx" && ext != ".csv")
            return BadRequest(new { ok = false, message = "Chỉ hỗ trợ định dạng .xlsx hoặc .csv." });

        var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "inventory");
        Directory.CreateDirectory(uploadDir);

        var safeName = $"nhapkho_{DateTime.Now:yyyyMMdd_HHmmss}_{Path.GetFileName(file.FileName)}";
        var fullPath = Path.Combine(uploadDir, safeName);

        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return Json(new { ok = true, url = $"/uploads/inventory/{safeName}", fileName = file.FileName });
    }

    private static List<string[]> ParseCsvRows(Stream stream)
    {
        var result = new List<string[]>();
        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, true);
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (!string.IsNullOrWhiteSpace(line))
                result.Add(line.Split(','));
        }
        return result;
    }

    private static List<string[]> ParseXlsxRows(Stream stream)
    {
        var result = new List<string[]>();
        using var archive = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Read, true);

        var sharedStrings = new List<string>();
        var ssEntry = archive.GetEntry("xl/sharedStrings.xml");
        if (ssEntry != null)
        {
            using var ssStream = ssEntry.Open();
            var ssDoc = System.Xml.Linq.XDocument.Load(ssStream);
            var ns = ssDoc.Root!.Name.Namespace;
            foreach (var si in ssDoc.Descendants(ns + "si"))
            {
                sharedStrings.Add(string.Concat(si.Descendants(ns + "t").Select(t => t.Value)));
            }
        }

        var sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml");
        if (sheetEntry == null) return result;

        using var sheetStream = sheetEntry.Open();
        var sheetDoc = System.Xml.Linq.XDocument.Load(sheetStream);
        var sNs = sheetDoc.Root!.Name.Namespace;

        foreach (var rowEl in sheetDoc.Descendants(sNs + "row"))
        {
            var cells = new SortedDictionary<int, string>();
            foreach (var c in rowEl.Elements(sNs + "c"))
            {
                var cellRef = c.Attribute("r")?.Value ?? "A1";
                var colLetters = new string(cellRef.TakeWhile(char.IsLetter).ToArray());
                int colIndex = 0;
                foreach (char ch in colLetters) colIndex = colIndex * 26 + (ch - 'A' + 1);
                colIndex = Math.Max(0, colIndex - 1);

                var type = c.Attribute("t")?.Value;
                var val = c.Element(sNs + "v")?.Value ?? "";
                if (type == "s" && int.TryParse(val, out var ssIdx) && ssIdx >= 0 && ssIdx < sharedStrings.Count)
                    val = sharedStrings[ssIdx];
                else if (type == "inlineStr")
                    val = string.Concat(c.Descendants(sNs + "t").Select(t => t.Value));

                cells[colIndex] = val;
            }

            if (cells.Count > 0)
            {
                int maxCol = cells.Keys.Max();
                var arr = new string[maxCol + 1];
                for (int i = 0; i <= maxCol; i++) arr[i] = cells.GetValueOrDefault(i, "");
                result.Add(arr);
            }
        }

        return result;
    }

    private static byte[] BuildSimpleXlsx(List<string[]> rows)
    {
        using var ms = new MemoryStream();
        using (var archive = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, true))
        {
            static void WriteEntry(System.IO.Compression.ZipArchive zip, string path, string xml)
            {
                var entry = zip.CreateEntry(path);
                using var writer = new StreamWriter(entry.Open(), new System.Text.UTF8Encoding(false));
                writer.Write(xml);
            }

            WriteEntry(archive, "[Content_Types].xml",
                @"<?xml version=""1.0"" encoding=""UTF-8""?><Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types""><Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/><Default Extension=""xml"" ContentType=""application/xml""/><Override PartName=""/xl/workbook.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/><Override PartName=""/xl/worksheets/sheet1.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/></Types>");

            WriteEntry(archive, "_rels/.rels",
                @"<?xml version=""1.0"" encoding=""UTF-8""?><Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships""><Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/></Relationships>");

            WriteEntry(archive, "xl/workbook.xml",
                @"<?xml version=""1.0"" encoding=""UTF-8""?><workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships""><sheets><sheet name=""NhapKho"" sheetId=""1"" r:id=""rId1""/></sheets></workbook>");

            WriteEntry(archive, "xl/_rels/workbook.xml.rels",
                @"<?xml version=""1.0"" encoding=""UTF-8""?><Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships""><Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet1.xml""/></Relationships>");

            var sb = new System.Text.StringBuilder();
            sb.Append(@"<?xml version=""1.0"" encoding=""UTF-8""?><worksheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main""><sheetData>");
            for (int r = 0; r < rows.Count; r++)
            {
                sb.Append($@"<row r=""{r + 1}"">");
                for (int c = 0; c < rows[r].Length; c++)
                {
                    var colName = ((char)('A' + c)).ToString();
                    var cellVal = rows[r][c] ?? "";
                    if (r > 0 && c >= 3 && int.TryParse(cellVal, out var num))
                        sb.Append($@"<c r=""{colName}{r + 1}""><v>{num}</v></c>");
                    else
                        sb.Append($@"<c r=""{colName}{r + 1}"" t=""inlineStr""><is><t>{System.Security.SecurityElement.Escape(cellVal)}</t></is></c>");
                }
                sb.Append("</row>");
            }
            sb.Append("</sheetData></worksheet>");

            WriteEntry(archive, "xl/worksheets/sheet1.xml", sb.ToString());
        }
        return ms.ToArray();
    }

    // =========================================================================
    // 3. QUẢN LÝ DANH MỤC SẢN PHẨM (PHẦN CỦA BẠN)
    // =========================================================================

    private static string NormalizeSizeType(string? rawSizeType, string categoryName, string? icon)
    {
        var val = (rawSizeType ?? "").Trim().ToUpperInvariant();
        return val switch
        {
            "LETTER" => "LETTER",
            "SHOE_ASIAN" or "NUMBER" or "SHOE" => "SHOE_ASIAN",
            "ACCESSORY" or "NONE" or "ACC" => "ACCESSORY",
            _ => InferSizeTypeFromNameOrIcon(categoryName, icon)
        };
    }

    private static string InferSizeTypeFromNameOrIcon(string categoryName, string? icon)
    {
        var ic = (icon ?? "").Trim();
        if (ic == "👟") return "SHOE_ASIAN";
        if (ic is "👜" or "🧢" or "🕶" or "🧦") return "ACCESSORY";

        var lower = (categoryName ?? "").ToLowerInvariant();
        if (lower.Contains("giày") || lower.Contains("dép") || lower.Contains("sneaker") || lower.Contains("boot") || lower.Contains("sandal"))
            return "SHOE_ASIAN";
        if (lower.Contains("túi") || lower.Contains("phụ kiện") || lower.Contains("mũ") || lower.Contains("nón") || lower.Contains("thắt lưng") || lower.Contains("kính") || lower.Contains("tất") || lower.Contains("vớ"))
            return "ACCESSORY";

        return "LETTER";
    }

    private static string InferIconFromName(string categoryName)
    {
        var lower = (categoryName ?? "").ToLowerInvariant();
        return lower.Contains("giày") || lower.Contains("dép") ? "👟"
             : lower.Contains("túi") ? "👜"
             : lower.Contains("mũ") || lower.Contains("nón") ? "🧢"
             : lower.Contains("phụ kiện") || lower.Contains("kính") || lower.Contains("thắt lưng") ? "🕶"
             : lower.Contains("tất") || lower.Contains("vớ") || lower.Contains("đồ lót") ? "🧦"
             : lower.Contains("quần") ? "👖"
             : lower.Contains("váy") || lower.Contains("đầm") || lower.Contains("chân váy") ? "👗"
             : lower.Contains("khoác") ? "🧥"
             : lower.Contains("sơ mi") ? "👔"
             : lower.Contains("áo") ? "👕"
             : "◫";
    }

    private static string EncodeCategoryMeta(string? icon, string? sizeType, string categoryName, string? description)
    {
        var chosenIcon = string.IsNullOrWhiteSpace(icon) ? InferIconFromName(categoryName) : icon.Trim();
        var chosenSize = NormalizeSizeType(sizeType, categoryName, chosenIcon);
        var cleanDesc = (description ?? "").Trim();

        var prefix = $"[ICON:{chosenIcon}][SIZE:{chosenSize}]";
        return string.IsNullOrEmpty(cleanDesc) ? prefix : $"{prefix} {cleanDesc}";
    }

    private static CategoryMetaInfo ParseCategoryMeta(string categoryName, string? rawDesc)
    {
        var text = rawDesc ?? "";
        string? icon = null;
        string? sizeType = null;

        while (text.StartsWith("["))
        {
            var endIdx = text.IndexOf(']');
            if (endIdx <= 1) break;

            var tag = text.Substring(1, endIdx - 1);
            if (tag.StartsWith("ICON:", StringComparison.OrdinalIgnoreCase))
            {
                icon = tag.Substring(5).Trim();
                text = text.Substring(endIdx + 1).TrimStart();
            }
            else if (tag.StartsWith("SIZE:", StringComparison.OrdinalIgnoreCase))
            {
                sizeType = tag.Substring(5).Trim().ToUpperInvariant();
                text = text.Substring(endIdx + 1).TrimStart();
            }
            else
            {
                break;
            }
        }

        var finalIcon = string.IsNullOrWhiteSpace(icon) ? InferIconFromName(categoryName) : icon;
        var finalSizeType = NormalizeSizeType(sizeType, categoryName, finalIcon);

        return new CategoryMetaInfo
        {
            Icon = finalIcon,
            SizeType = finalSizeType,
            Description = text
        };
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Categories()
    {
        var danhMucs = await _context.DanhMucs
            .Include(dm => dm.SanPhams)
            .OrderBy(dm => dm.Id)
            .ToListAsync();

        ViewBag.CategoryMeta = danhMucs.ToDictionary(
            dm => dm.Id,
            dm => ParseCategoryMeta(dm.TenDanhMuc, dm.MoTa));

        var items = danhMucs.Select(dm => new CategoryViewModel
        {
            Id = dm.Id,
            Name = dm.TenDanhMuc,
            ProductCount = dm.SanPhams.Count,
            Status = dm.TrangThai ? "Đang hoạt động" : "Tạm ẩn"
        }).ToList();

        return View(items);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(string tenDanhMuc, string? icon, string? loaiSize, string? sizeType, string? moTa)
    {
        if (string.IsNullOrWhiteSpace(tenDanhMuc))
        {
            TempData["CategoryError"] = "Tên danh mục không được để trống.";
            return RedirectToAction(nameof(Categories));
        }

        var ten = tenDanhMuc.Trim();
        if (await _context.DanhMucs.AnyAsync(d => d.TenDanhMuc == ten))
        {
            TempData["CategoryError"] = "Danh mục này đã tồn tại.";
            return RedirectToAction(nameof(Categories));
        }

        var chosenSize = !string.IsNullOrWhiteSpace(loaiSize) ? loaiSize : sizeType;

        _context.DanhMucs.Add(new DanhMuc
        {
            TenDanhMuc = ten,
            MoTa = EncodeCategoryMeta(icon, chosenSize, ten, moTa),
            TrangThai = true,
            ThoiGianTao = DateTime.Now
        });
        await _context.SaveChangesAsync();

        TempData["CategorySuccess"] = $"Đã thêm danh mục \"{ten}\" thành công.";
        return RedirectToAction(nameof(Categories));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(int id, string tenDanhMuc, string? icon, string? loaiSize, string? sizeType, string? moTa)
    {
        var dm = await _context.DanhMucs.FindAsync(id);
        if (dm == null || string.IsNullOrWhiteSpace(tenDanhMuc))
        {
            TempData["CategoryError"] = "Dữ liệu cập nhật danh mục không hợp lệ.";
            return RedirectToAction(nameof(Categories));
        }

        var ten = tenDanhMuc.Trim();
        if (await _context.DanhMucs.AnyAsync(d => d.TenDanhMuc == ten && d.Id != id))
        {
            TempData["CategoryError"] = "Tên danh mục này đã trùng với một danh mục khác.";
            return RedirectToAction(nameof(Categories));
        }

        var existingMeta = ParseCategoryMeta(ten, dm.MoTa);
        var chosenSize = !string.IsNullOrWhiteSpace(loaiSize) ? loaiSize : sizeType;
        var effectiveSizeType = !string.IsNullOrWhiteSpace(chosenSize) ? chosenSize : existingMeta.SizeType;

        dm.TenDanhMuc = ten;
        dm.MoTa = EncodeCategoryMeta(icon, effectiveSizeType, ten, moTa);
        await _context.SaveChangesAsync();

        TempData["CategorySuccess"] = $"Đã cập nhật danh mục \"{ten}\" thành công.";
        return RedirectToAction(nameof(Categories));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCategory(int id)
    {
        var dm = await _context.DanhMucs.FindAsync(id);
        if (dm != null)
        {
            dm.TrangThai = !dm.TrangThai;
            await _context.SaveChangesAsync();
            TempData["CategorySuccess"] = $"Đã chuyển danh mục \"{dm.TenDanhMuc}\" sang {(dm.TrangThai ? "Đang hoạt động" : "Tạm ẩn")}.";
        }
        return RedirectToAction(nameof(Categories));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var dm = await _context.DanhMucs
            .Include(d => d.SanPhams)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (dm == null) return RedirectToAction(nameof(Categories));

        if (dm.SanPhams.Count > 0)
        {
            TempData["CategoryError"] = $"Không thể xóa danh mục \"{dm.TenDanhMuc}\" vì đang có {dm.SanPhams.Count} sản phẩm. Hãy chuyển sang Tạm ẩn hoặc dời sản phẩm trước.";
            return RedirectToAction(nameof(Categories));
        }

        _context.DanhMucs.Remove(dm);
        await _context.SaveChangesAsync();
        TempData["CategorySuccess"] = $"Đã xóa danh mục \"{dm.TenDanhMuc}\" thành công.";
        return RedirectToAction(nameof(Categories));
    }

    // =========================================================================
    // 4. QUẢN LÝ SẢN PHẨM & BIẾN THỂ (PHẦN CỦA BẠN)
    // =========================================================================

    private async Task<int> ResolveMauSacIdAsync(int? mauSacId, string? tenMauMoi)
    {
        var cleanName = (tenMauMoi ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(cleanName))
        {
            var existing = await _context.MauSacs
                .FirstOrDefaultAsync(m => m.TenMau.ToLower() == cleanName.ToLower());

            if (existing != null)
                return existing.Id;

            var newColor = new MauSac
            {
                TenMau = cleanName
            };
            _context.MauSacs.Add(newColor);
            await _context.SaveChangesAsync();
            return newColor.Id;
        }

        if (mauSacId.HasValue && mauSacId.Value > 0)
            return mauSacId.Value;

        var fallback = await _context.MauSacs.FirstOrDefaultAsync();
        return fallback?.Id ?? 1;
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Products(string? search, int? categoryId, string? status)
    {
        ViewBag.Categories = await _context.DanhMucs.Where(d => d.TrangThai).ToListAsync();
        ViewBag.Sizes = await _context.KichThuocs.OrderBy(k => k.ThuTu).ToListAsync();
        ViewBag.Colors = await _context.MauSacs.OrderBy(m => m.TenMau).ToListAsync();
        ViewBag.Search = search;
        ViewBag.SelectedCategory = categoryId;
        ViewBag.SelectedStatus = status;

        var query = _context.SanPhams
            .Include(sp => sp.DanhMuc)
            .Include(sp => sp.SanPhamAnhs)
            .Include(sp => sp.BienTheSanPhams)
                .ThenInclude(bt => bt.KichThuoc)
            .Include(sp => sp.BienTheSanPhams)
                .ThenInclude(bt => bt.MauSac)
            .Include(sp => sp.BienTheSanPhams)
                .ThenInclude(bt => bt.TonKhos)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var kw = search.Trim();
            query = query.Where(sp => sp.TenSanPham.Contains(kw) || sp.MaSanPham.Contains(kw));
        }

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(sp => sp.DanhMucId == categoryId.Value);
        }

        var sanPhams = await query
            .OrderByDescending(sp => sp.ThoiGianTao)
            .ToListAsync();

        var since30Days = DateTime.Now.AddDays(-30);
        var sold30Dict = await _context.DonHangChiTiets
            .Where(ct => ct.DonHang.ThoiGianTao >= since30Days && ct.DonHang.TrangThai != "Da_huy")
            .GroupBy(ct => ct.BienThe.SanPhamId)
            .Select(g => new { SanPhamId = g.Key, Qty = g.Sum(x => x.SoLuong) })
            .ToDictionaryAsync(x => x.SanPhamId, x => x.Qty);

        var items = sanPhams.Select(sp =>
        {
            var activeVariants = sp.BienTheSanPhams.Where(bt => bt.TrangThai).ToList();
            var stock = activeVariants.Sum(bt => bt.TonKhos.Sum(tk => tk.SoLuongTon - tk.SoLuongGiuCho));
            var price = activeVariants.Count > 0 ? activeVariants.Min(bt => bt.GiaBan) : sp.GiaBan;
            var image = sp.SanPhamAnhs
                .OrderByDescending(a => a.LaAnhDaiDien)
                .Select(a => a.Url)
                .FirstOrDefault() ?? "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=200";

            var st = sp.TrangThai != "Dang_ban"
                ? "Ngừng bán"
                : (stock == 0 ? "Hết hàng" : (stock <= 5 ? "Sắp hết" : "Đang bán"));

            return new ProductViewModel
            {
                Id = sp.Id,
                Name = sp.TenSanPham,
                Category = sp.DanhMuc?.TenDanhMuc ?? "Khác",
                Price = price,
                Stock = stock,
                Status = st,
                Image = image
            };
        }).ToList();

        if (!string.IsNullOrWhiteSpace(status))
        {
            items = items.Where(i => i.Status == status).ToList();
        }

        ViewBag.ProductMeta = sanPhams.ToDictionary(
            sp => sp.Id,
            sp =>
            {
                var cost = sp.GiaGoc > 0 ? sp.GiaGoc : sp.GiaBan;
                var marginPct = cost > 0 ? Math.Round((sp.GiaBan - cost) * 100m / cost, 1) : 0m;
                return new
                {
                    sp.MaSanPham,
                    sp.DanhMucId,
                    GiaGoc = cost,
                    sp.GiaBan,
                    TyLeLoiNhuan = marginPct,
                    LoiNhuanDonVi = sp.GiaBan - cost,
                    MoTa = sp.MoTa ?? "",
                    Image = sp.SanPhamAnhs
                        .OrderByDescending(a => a.LaAnhDaiDien)
                        .Select(a => a.Url)
                        .FirstOrDefault() ?? ""
                };
            });

        var pricingAdvices = new Dictionary<int, ProductTrendAdvice>();
        foreach (var sp in sanPhams)
        {
            var cost = sp.GiaGoc > 0 ? sp.GiaGoc : sp.GiaBan;
            var currentMargin = cost > 0 ? Math.Round((sp.GiaBan - cost) * 100m / cost, 1) : 0m;
            var availableStock = sp.BienTheSanPhams
                .Where(bt => bt.TrangThai)
                .Sum(bt => bt.TonKhos.Sum(tk => tk.SoLuongTon - tk.SoLuongGiuCho));
            var sold30 = sold30Dict.TryGetValue(sp.Id, out var sQty) ? sQty : 0;

            if (sp.TrangThai != "Dang_ban") continue;

            if (sold30 >= 5)
            {
                var boost = sold30 >= 10 ? 8m : 5m;
                var suggestedMargin = currentMargin + boost;
                var suggestedPrice = Math.Round(cost * (1 + suggestedMargin / 100m) / 1000m) * 1000m;
                pricingAdvices[sp.Id] = new ProductTrendAdvice
                {
                    ProductId = sp.Id,
                    ProductName = sp.TenSanPham,
                    ProductCode = sp.MaSanPham,
                    TrendType = "hot",
                    TrendLabel = "🔥 Bán chạy",
                    Summary = $"Bán {sold30} sp trong 30 ngày • Tồn còn {availableStock}",
                    CurrentMargin = currentMargin,
                    SuggestedMargin = suggestedMargin,
                    CurrentPrice = sp.GiaBan,
                    SuggestedPrice = suggestedPrice,
                    ActionHint = $"Tăng lợi nhuận từ {currentMargin:0.#}% lên {suggestedMargin:0.#}% (+{(suggestedPrice - sp.GiaBan):N0} đ/sp)"
                };
            }
            else if (availableStock >= 8 && sold30 <= 1)
            {
                var safeFloorPrice = Math.Round(cost * 1.05m / 1000m) * 1000m;
                var maxSafeDiscount = sp.GiaBan > safeFloorPrice
                    ? (int)Math.Floor((sp.GiaBan - safeFloorPrice) * 100m / sp.GiaBan)
                    : 0;
                var suggestedDiscount = Math.Min(25, Math.Max(5, (maxSafeDiscount / 5) * 5));
                var promoCode = $"KICHCAU-{sp.MaSanPham.Replace("SP-", "")}";
                var suggestedMargin = Math.Max(8m, Math.Round(currentMargin - 7m, 1));
                var suggestedPrice = Math.Round(cost * (1 + suggestedMargin / 100m) / 1000m) * 1000m;

                pricingAdvices[sp.Id] = new ProductTrendAdvice
                {
                    ProductId = sp.Id,
                    ProductName = sp.TenSanPham,
                    ProductCode = sp.MaSanPham,
                    TrendType = "slow",
                    TrendLabel = "🐢 Bán chậm • Tồn cao",
                    Summary = $"Chỉ bán {sold30} sp / 30 ngày • Đang tồn {availableStock} sp",
                    CurrentMargin = currentMargin,
                    SuggestedMargin = suggestedMargin,
                    CurrentPrice = sp.GiaBan,
                    SuggestedPrice = suggestedPrice,
                    SuggestedDiscountPercent = suggestedDiscount,
                    SuggestedPromoCode = promoCode,
                    ActionHint = $"Hạ lợi nhuận còn {suggestedMargin:0.#}% hoặc tung mã giảm {suggestedDiscount}% (vẫn lãi trên giá gốc)"
                };
            }
        }

        ViewBag.PricingAdvices = pricingAdvices;

        ViewBag.ProductVariants = sanPhams.ToDictionary(
            sp => sp.Id,
            sp => sp.BienTheSanPhams
                .OrderBy(bt => bt.KichThuoc?.ThuTu ?? 999)
                .ThenBy(bt => bt.MauSac?.TenMau)
                .Select(bt => new ProductVariantInfo
                {
                    Id = bt.Id,
                    Sku = bt.Sku,
                    KichThuocId = bt.KichThuocId,
                    Size = bt.KichThuoc?.MaSize ?? "—",
                    MauSacId = bt.MauSacId,
                    Color = bt.MauSac?.TenMau ?? "—",
                    Price = bt.GiaBan,
                    Stock = bt.TonKhos.Sum(tk => tk.SoLuongTon),
                    Reserved = bt.TonKhos.Sum(tk => tk.SoLuongGiuCho),
                    Available = bt.TonKhos.Sum(tk => tk.SoLuongTon - tk.SoLuongGiuCho),
                    TrangThai = bt.TrangThai
                })
                .ToList());

        return View(items);
    }

  [Authorize(Roles = "Admin")]
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CreateProduct(
    string tenSanPham,
    int danhMucId,
    decimal giaGoc,
    decimal tyLeLoiNhuan,
    decimal? giaBan,
    int kichThuocId,
    int? mauSacId,
    string? tenMauMoi,
    int soLuongTon,
    string? imageUrl,
    string? moTa)
{
    if (string.IsNullOrWhiteSpace(tenSanPham) || giaGoc <= 0)
    {
        TempData["ProductError"] = "Vui lòng nhập tên sản phẩm và giá gốc hợp lệ (> 0 đ).";
        return RedirectToAction(nameof(Products));
    }

    var safeMargin = Math.Max(0m, tyLeLoiNhuan);
    var computedPrice = Math.Round(giaGoc * (1m + safeMargin / 100m) / 1000m) * 1000m;
    var rawPrice = (giaBan.HasValue && giaBan.Value > 0) ? giaBan.Value : computedPrice;
    var finalPrice = Math.Max(giaGoc, rawPrice);
    var effectiveMargin = giaGoc > 0m
        ? Math.Max(0m, Math.Round(((finalPrice - giaGoc) * 100m / giaGoc) * 10m) / 10m)
        : 0m;

    var resolvedColorId = await ResolveMauSacIdAsync(mauSacId, tenMauMoi);

    var maSp = "SP-" + DateTime.Now.ToString("yyMMddHHmmss");
    var sp = new SanPham
    {
        MaSanPham = maSp,
        TenSanPham = tenSanPham.Trim(),
        DanhMucId = danhMucId,
        MoTa = moTa?.Trim(),
        GiaGoc = giaGoc,
        GiaBan = finalPrice,
        TrangThai = "Dang_ban",
        ThoiGianTao = DateTime.Now,
        ThoiGianCapNhat = DateTime.Now
    };
    _context.SanPhams.Add(sp);
    await _context.SaveChangesAsync();

    var bienThe = new BienTheSanPham
    {
        SanPhamId = sp.Id,
        Sku = $"{maSp}-S{kichThuocId}-M{resolvedColorId}",
        KichThuocId = kichThuocId,
        MauSacId = resolvedColorId,
        GiaBan = finalPrice,
        TrangThai = true
    };
    _context.BienTheSanPhams.Add(bienThe);
    await _context.SaveChangesAsync();

    var khoMacDinh = await _context.Khos.FirstOrDefaultAsync(k => k.TrangThai);
    if (khoMacDinh != null)
    {
        _context.TonKhos.Add(new TonKho
        {
            KhoId = khoMacDinh.Id,
            BienTheId = bienThe.Id,
            SoLuongTon = Math.Max(0, soLuongTon),
            SoLuongGiuCho = 0
        });
    }

    var finalImg = string.IsNullOrWhiteSpace(imageUrl)
        ? "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=600"
        : imageUrl.Trim();

    _context.SanPhamAnhs.Add(new SanPhamAnh
    {
        SanPhamId = sp.Id,
        Url = finalImg,
        LaAnhDaiDien = true
    });

    await _context.SaveChangesAsync();
    var profitPerUnit = Math.Max(0m, finalPrice - giaGoc);
    TempData["ProductSuccess"] = $"Đã tạo \"{sp.TenSanPham}\" — Giá gốc: {giaGoc:N0} đ | Giá bán: {finalPrice:N0} đ (Lãi {effectiveMargin:0.#}%, +{profitPerUnit:N0} đ/sp).";
    return RedirectToAction(nameof(Products));
}

[Authorize(Roles = "Admin")]
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> EditProduct(
    int id,
    string tenSanPham,
    int danhMucId,
    decimal giaGoc,
    decimal tyLeLoiNhuan,
    decimal? giaBan,
    string? imageUrl,
    string? moTa)
{
    var sp = await _context.SanPhams
        .Include(s => s.SanPhamAnhs)
        .Include(s => s.BienTheSanPhams)
        .FirstOrDefaultAsync(s => s.Id == id);

    if (sp == null || string.IsNullOrWhiteSpace(tenSanPham) || giaGoc <= 0)
    {
        TempData["ProductError"] = "Thông tin cập nhật sản phẩm không hợp lệ.";
        return RedirectToAction(nameof(Products));
    }

    var safeMargin = Math.Max(0m, tyLeLoiNhuan);
    var computedPrice = Math.Round(giaGoc * (1m + safeMargin / 100m) / 1000m) * 1000m;
    var rawPrice = (giaBan.HasValue && giaBan.Value > 0) ? giaBan.Value : computedPrice;
    var finalPrice = Math.Max(giaGoc, rawPrice);
    var effectiveMargin = giaGoc > 0m
        ? Math.Max(0m, Math.Round(((finalPrice - giaGoc) * 100m / giaGoc) * 10m) / 10m)
        : 0m;

    sp.TenSanPham = tenSanPham.Trim();
    sp.DanhMucId = danhMucId;
    sp.GiaGoc = giaGoc;
    sp.GiaBan = finalPrice;
    sp.MoTa = moTa?.Trim();
    sp.ThoiGianCapNhat = DateTime.Now;

    foreach (var bt in sp.BienTheSanPhams)
    {
        bt.GiaBan = finalPrice;
    }

    if (!string.IsNullOrWhiteSpace(imageUrl))
    {
        var mainImg = sp.SanPhamAnhs.OrderByDescending(a => a.LaAnhDaiDien).FirstOrDefault();
        if (mainImg != null)
        {
            mainImg.Url = imageUrl.Trim();
        }
        else
        {
            _context.SanPhamAnhs.Add(new SanPhamAnh
            {
                SanPhamId = sp.Id,
                Url = imageUrl.Trim(),
                LaAnhDaiDien = true
            });
        }
    }

    await _context.SaveChangesAsync();
    var profitPerUnit = Math.Max(0m, finalPrice - giaGoc);
    TempData["ProductSuccess"] = $"Đã cập nhật \"{sp.TenSanPham}\" — Giá bán mới: {finalPrice:N0} đ (Lãi {effectiveMargin:0.#}%, +{profitPerUnit:N0} đ/sp).";
    return RedirectToAction(nameof(Products));
}

[Authorize(Roles = "Admin")]
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ApplyProfitSuggestion(int id, decimal newMarginPercent)
{
    var sp = await _context.SanPhams
        .Include(s => s.BienTheSanPhams)
        .FirstOrDefaultAsync(s => s.Id == id);

    if (sp == null) return RedirectToAction(nameof(Products));

    var cost = sp.GiaGoc > 0 ? sp.GiaGoc : sp.GiaBan;
    var safeMargin = Math.Max(0m, newMarginPercent);
    var newPrice = Math.Max(cost, Math.Round(cost * (1m + safeMargin / 100m) / 1000m) * 1000m);

    sp.GiaGoc = cost;
    sp.GiaBan = newPrice;
    sp.ThoiGianCapNhat = DateTime.Now;
    foreach (var bt in sp.BienTheSanPhams)
    {
        bt.GiaBan = newPrice;
    }

    await _context.SaveChangesAsync();
    TempData["ProductSuccess"] = $"Đã điều chỉnh lợi nhuận \"{sp.TenSanPham}\" thành {safeMargin:0.#}% → Giá bán mới: {newPrice:N0} đ.";
    return RedirectToAction(nameof(Products));
}
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var sp = await _context.SanPhams
            .Include(s => s.BienTheSanPhams)
                .ThenInclude(bt => bt.DonHangChiTiets)
            .Include(s => s.BienTheSanPhams)
                .ThenInclude(bt => bt.TonKhos)
            .Include(s => s.SanPhamAnhs)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sp == null) return RedirectToAction(nameof(Products));

        bool hasOrders = sp.BienTheSanPhams.Any(bt => bt.DonHangChiTiets.Count > 0);
        if (hasOrders)
        {
            sp.TrangThai = "Ngung_ban";
            sp.ThoiGianCapNhat = DateTime.Now;
            foreach (var bt in sp.BienTheSanPhams) bt.TrangThai = false;
            await _context.SaveChangesAsync();
            TempData["ProductSuccess"] = $"Sản phẩm \"{sp.TenSanPham}\" đã có lịch sử đơn hàng nên hệ thống chuyển sang trạng thái Ngừng bán.";
            return RedirectToAction(nameof(Products));
        }

        foreach (var bt in sp.BienTheSanPhams)
        {
            _context.TonKhos.RemoveRange(bt.TonKhos);
        }
        _context.BienTheSanPhams.RemoveRange(sp.BienTheSanPhams);
        _context.SanPhamAnhs.RemoveRange(sp.SanPhamAnhs);
        _context.SanPhams.Remove(sp);
        await _context.SaveChangesAsync();

        TempData["ProductSuccess"] = $"Đã xóa sản phẩm \"{sp.TenSanPham}\" khỏi hệ thống.";
        return RedirectToAction(nameof(Products));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddVariant(int sanPhamId, int kichThuocId, int? mauSacId, string? tenMauMoi, decimal giaBan)
    {
        var sp = await _context.SanPhams
            .Include(s => s.BienTheSanPhams)
            .FirstOrDefaultAsync(s => s.Id == sanPhamId);

        if (sp == null || giaBan <= 0)
        {
            TempData["ProductError"] = "Thông tin phân loại không hợp lệ.";
            return RedirectToAction(nameof(Products));
        }

        var resolvedColorId = await ResolveMauSacIdAsync(mauSacId, tenMauMoi);

        if (sp.BienTheSanPhams.Any(bt => bt.KichThuocId == kichThuocId && bt.MauSacId == resolvedColorId))
        {
            TempData["ProductError"] = "Cặp Size và Màu sắc này đã tồn tại trong sản phẩm.";
            TempData["OpenVariantProductId"] = sanPhamId;
            return RedirectToAction(nameof(Products));
        }

        var newVariant = new BienTheSanPham
        {
            SanPhamId = sanPhamId,
            Sku = $"{sp.MaSanPham}-S{kichThuocId}-M{resolvedColorId}-{DateTime.Now:mmss}",
            KichThuocId = kichThuocId,
            MauSacId = resolvedColorId,
            GiaBan = giaBan,
            TrangThai = true
        };
        _context.BienTheSanPhams.Add(newVariant);
        await _context.SaveChangesAsync();

        var khoMacDinh = await _context.Khos.FirstOrDefaultAsync(k => k.TrangThai);
        if (khoMacDinh != null)
        {
            _context.TonKhos.Add(new TonKho
            {
                KhoId = khoMacDinh.Id,
                BienTheId = newVariant.Id,
                SoLuongTon = 0,
                SoLuongGiuCho = 0
            });
            await _context.SaveChangesAsync();
        }

        TempData["ProductSuccess"] = $"Đã thêm phân loại mới ({newVariant.Sku}) với tồn kho ban đầu = 0.";
        TempData["OpenVariantProductId"] = sanPhamId;
        return RedirectToAction(nameof(Products));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditVariant(long variantId, int sanPhamId, int kichThuocId, int? mauSacId, string? tenMauMoi, decimal giaBan, bool trangThai)
    {
        var bt = await _context.BienTheSanPhams.FirstOrDefaultAsync(x => x.Id == variantId && x.SanPhamId == sanPhamId);
        if (bt == null || giaBan <= 0)
        {
            TempData["ProductError"] = "Không tìm thấy phân loại cần cập nhật.";
            return RedirectToAction(nameof(Products));
        }

        var resolvedColorId = await ResolveMauSacIdAsync(mauSacId, tenMauMoi);

        bool duplicate = await _context.BienTheSanPhams.AnyAsync(x =>
            x.SanPhamId == sanPhamId && x.Id != variantId && x.KichThuocId == kichThuocId && x.MauSacId == resolvedColorId);
        if (duplicate)
        {
            TempData["ProductError"] = "Cặp Size và Màu sắc này bị trùng với phân loại khác.";
            TempData["OpenVariantProductId"] = sanPhamId;
            return RedirectToAction(nameof(Products));
        }

        bt.KichThuocId = kichThuocId;
        bt.MauSacId = resolvedColorId;
        bt.GiaBan = giaBan;
        bt.TrangThai = trangThai;
        await _context.SaveChangesAsync();

        TempData["ProductSuccess"] = $"Đã cập nhật phân loại {bt.Sku}.";
        TempData["OpenVariantProductId"] = sanPhamId;
        return RedirectToAction(nameof(Products));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteVariant(long variantId, int sanPhamId)
    {
        var bt = await _context.BienTheSanPhams
            .Include(x => x.DonHangChiTiets)
            .Include(x => x.TonKhos)
            .FirstOrDefaultAsync(x => x.Id == variantId && x.SanPhamId == sanPhamId);

        if (bt == null) return RedirectToAction(nameof(Products));

        if (bt.DonHangChiTiets.Count > 0)
        {
            bt.TrangThai = false;
            await _context.SaveChangesAsync();
            TempData["ProductSuccess"] = $"Phân loại {bt.Sku} đã phát sinh đơn hàng nên được chuyển sang Tạm ẩn.";
        }
        else
        {
            _context.TonKhos.RemoveRange(bt.TonKhos);
            _context.BienTheSanPhams.Remove(bt);
            await _context.SaveChangesAsync();
            TempData["ProductSuccess"] = $"Đã xóa phân loại {bt.Sku}.";
        }

        TempData["OpenVariantProductId"] = sanPhamId;
        return RedirectToAction(nameof(Products));
    }

    // =========================================================================
    // 5. NGƯỜI DÙNG (THEO origin/main ĐỂ KHỚP VIEW Users.cshtml)
    // =========================================================================

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

    // =========================================================================
    // 6. NHÂN VIÊN (THEO origin/main ĐỂ KHỚP VIEW Employees.cshtml)
    // =========================================================================

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

    // =========================================================================
    // 7. KHUYẾN MÃI (THEO origin/main ĐỂ KHỚP VIEW Promotions.cshtml)
    // =========================================================================

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
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> EditPromotion(
    long id,
    string maVoucher,
    string moTa,
    string loaiGiam,
    decimal giaTri,
    decimal donToiThieu,
    int soLuongPhatHanh,
    DateTime ngayBatDau,
    DateTime ngayKetThuc,
    bool trangThai = false)
{
    var promo = await _context.MaGiamGia.FirstOrDefaultAsync(x => x.Id == id);
    if (promo == null)
    {
        TempData["PromoError"] = "Không tìm thấy mã khuyến mãi cần sửa.";
        return RedirectToAction(nameof(Promotions));
    }

    var codeClean = (maVoucher ?? "").Trim().ToUpperInvariant();
    if (string.IsNullOrWhiteSpace(codeClean))
    {
        TempData["PromoError"] = "Mã voucher không được để trống.";
        return RedirectToAction(nameof(Promotions));
    }

    var duplicate = await _context.MaGiamGia.AnyAsync(x => x.Id != id && x.MaVoucher == codeClean);
    if (duplicate)
    {
        TempData["PromoError"] = $"Mã voucher '{codeClean}' đã tồn tại.";
        return RedirectToAction(nameof(Promotions));
    }

    if (ngayKetThuc < ngayBatDau)
    {
        TempData["PromoError"] = "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.";
        return RedirectToAction(nameof(Promotions));
    }

    promo.MaVoucher = codeClean;
    promo.MoTa = (moTa ?? "").Trim();
    promo.LoaiGiam = loaiGiam == "Tien_mat" ? "Tien_mat" : "Phan_tram";
    promo.GiaTri = giaTri;
    promo.DonToiThieu = Math.Max(0, donToiThieu);
    promo.SoLuongPhatHanh = Math.Max(promo.SoLuongDaDung, soLuongPhatHanh);
    promo.NgayBatDau = ngayBatDau;
    promo.NgayKetThuc = ngayKetThuc;
    promo.TrangThai = trangThai;

    await _context.SaveChangesAsync();
    TempData["PromoSuccess"] = $"Đã cập nhật mã khuyến mãi {promo.MaVoucher}.";
    return RedirectToAction(nameof(Promotions));
}
    // =========================================================================
    // 8. ĐÁNH GIÁ SẢN PHẨM (THEO origin/main ĐỂ KHỚP VIEW Reviews.cshtml)
    // =========================================================================

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

    // =========================================================================
    // 9. BÁO CÁO & THỐNG KÊ DOANH THU + XUẤT PDF (PHẦN CỦA BẠN)
    // =========================================================================

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reports(int? year, int? month)
    {
        var currentYear = DateTime.Today.Year;

        var minOrderYear = await _context.DonHangs
            .Select(d => (int?)d.ThoiGianTao.Year)
            .MinAsync();

        var minUserYear = await _context.NguoiDungs
            .Select(u => (int?)u.ThoiGianTao.Year)
            .MinAsync();

        var candidateYears = new[] { minOrderYear, minUserYear }
            .Where(y => y.HasValue && y.Value > 2000 && y.Value <= currentYear)
            .Select(y => y!.Value)
            .ToList();

        var startYear = candidateYears.Count > 0 ? candidateYears.Min() : currentYear;

        var selectedYear = (year.HasValue && year.Value >= startYear && year.Value <= currentYear)
            ? year.Value
            : currentYear;

        var selectedMonth = (month is >= 1 and <= 12)
            ? month.Value
            : (selectedYear == currentYear ? DateTime.Today.Month : 1);

        var data = await BuildReportSnapshotAsync(selectedYear, selectedMonth);

        ViewBag.StartYear = startYear;
        ViewBag.CurrentYear = currentYear;
        ViewBag.SelectedYear = selectedYear;
        ViewBag.SelectedMonth = selectedMonth;
        ViewBag.ReportData = data;
        ViewBag.SavedReports = GetSavedReportFiles();

        return View();
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReportDocument(int? year, int? month)
    {
        var selectedYear = year ?? DateTime.Today.Year;
        var selectedMonth = (month is >= 1 and <= 12) ? month.Value : DateTime.Today.Month;

        var data = await BuildReportSnapshotAsync(selectedYear, selectedMonth);
        return View(data);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExportReportPdf(int year, int month)
    {
        var selectedYear = year > 2000 ? year : DateTime.Today.Year;
        var selectedMonth = (month is >= 1 and <= 12) ? month : DateTime.Today.Month;

        var data = await BuildReportSnapshotAsync(selectedYear, selectedMonth);
        var creatorName = User.Identity?.Name ?? "Quản trị viên";

        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseSystemFonts = true;
        QuestPDF.Settings.ThrowOnMissingFontFamilies = false;

        var pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(32);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10).FontColor("#1E293B"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text("FASHIONHUB STORE").FontSize(15).Bold().FontColor("#4F46E5");
                            left.Item().Text("Hệ thống quản lý kinh doanh thời trang").FontSize(9).FontColor("#64748B");
                        });
                        row.ConstantItem(210).AlignRight().Column(right =>
                        {
                            right.Item().Text($"Mã báo cáo: {data.ReportCode}").FontSize(9.5f).Bold();
                            right.Item().Text($"Ngày lập: {data.GeneratedAt:dd/MM/yyyy HH:mm}").FontSize(9).FontColor("#64748B");
                            right.Item().Text($"Người lập: {creatorName}").FontSize(9).FontColor("#64748B");
                        });
                    });

                    col.Item().PaddingTop(10).BorderBottom(1.5f).BorderColor("#CBD5E1").PaddingBottom(8).AlignCenter().Column(titleCol =>
                    {
                        titleCol.Item().Text("BÁO CÁO TỔNG HỢP DOANH THU & BÁN HÀNG").FontSize(15).Bold().FontColor("#0F172A");
                        titleCol.Item().Text($"Kỳ báo cáo: Tháng {selectedMonth:00}/{selectedYear} (Lũy kế năm {selectedYear})").FontSize(10.5f).FontColor("#475569");
                    });
                });

                page.Content().PaddingVertical(14).Column(col =>
                {
                    col.Spacing(14);

                    col.Item().Text($"I. CHỈ SỐ HOẠT ĐỘNG THÁNG {selectedMonth:00}/{selectedYear}").FontSize(11).Bold().FontColor("#0F172A");
                    col.Item().Row(row =>
                    {
                        row.Spacing(8);
                        void KpiBox(string label, string val, string sub)
                        {
                            row.RelativeItem().Border(1).BorderColor("#CBD5E1").Background("#F8FAFC").Padding(9).Column(c =>
                            {
                                c.Item().Text(label).FontSize(8.5f).FontColor("#64748B");
                                c.Item().PaddingVertical(2).Text(val).FontSize(12.5f).Bold().FontColor("#0F172A");
                                c.Item().Text(sub).FontSize(8).FontColor("#4F46E5");
                            });
                        }

                        KpiBox("DOANH THU THÁNG", $"{data.MonthRevenue:N0} đ", $"Cả năm: {data.YearRevenue:N0} đ");
                        KpiBox("ĐƠN HÀNG HỢP LỆ", $"{data.MonthOrders} đơn", $"Hoàn tất: {data.CompletedOrders} | Hủy: {data.CancelledOrders}");
                        KpiBox("KHÁCH HÀNG MỚI", $"{data.NewCustomers} khách", "Tài khoản đăng ký mới");
                        KpiBox("GIÁ TRỊ ĐƠN TB", $"{data.AvgOrder:N0} đ", "Trung bình / đơn hợp lệ");
                    });

                    col.Item().Text($"II. BẢNG THỐNG KÊ DOANH THU 12 THÁNG NĂM {selectedYear}").FontSize(11).Bold().FontColor("#0F172A");
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(65);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            void HCell(string text, bool right = false)
                            {
                                var cell = header.Cell().Background("#1E293B").Padding(6);
                                if (right) cell.AlignRight().Text(text).FontColor(Colors.White).Bold().FontSize(9);
                                else cell.Text(text).FontColor(Colors.White).Bold().FontSize(9);
                            }
                            HCell("Tháng");
                            HCell("Số đơn hàng", true);
                            HCell("Doanh thu (VNĐ)", true);
                            HCell("Giá trị TB / đơn", true);
                        });

                        foreach (var m in data.MonthlyBreakdown)
                        {
                            var bg = m.Month == selectedMonth ? "#EEF2FF" : (m.Month % 2 == 0 ? "#F8FAFC" : "#FFFFFF");

                            var monthText = table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(5).Text($"Tháng {m.Month:00}");
                            if (m.Month == selectedMonth) monthText.Bold();

                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(5).AlignRight().Text($"{m.OrderCount:N0}");

                            var revText = table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(5).AlignRight().Text($"{m.Revenue:N0} đ");
                            if (m.Revenue > 0) revText.Bold();

                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(5).AlignRight().Text($"{m.AvgOrder:N0} đ");
                        }
                    });

                    col.Item().Text($"III. TOP SẢN PHẨM BÁN CHẠY NĂM {selectedYear}").FontSize(11).Bold().FontColor("#0F172A");
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(36);
                            columns.RelativeColumn(3.5f);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1.8f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background("#1E293B").Padding(6).Text("STT").FontColor(Colors.White).Bold().FontSize(9);
                            header.Cell().Background("#1E293B").Padding(6).Text("Tên sản phẩm").FontColor(Colors.White).Bold().FontSize(9);
                            header.Cell().Background("#1E293B").Padding(6).AlignRight().Text("Đã bán").FontColor(Colors.White).Bold().FontSize(9);
                            header.Cell().Background("#1E293B").Padding(6).AlignRight().Text("Doanh thu").FontColor(Colors.White).Bold().FontSize(9);
                        });

                        if (data.TopProducts.Count == 0)
                        {
                            table.Cell().ColumnSpan(4).Padding(8).AlignCenter().Text("Chưa có dữ liệu bán hàng trong kỳ.").FontColor("#64748B");
                        }
                        else
                        {
                            for (int i = 0; i < data.TopProducts.Count; i++)
                            {
                                var p = data.TopProducts[i];
                                table.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(5).Text($"{i + 1}");
                                table.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(5).Text(p.Name);
                                table.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(5).AlignRight().Text($"{p.Stock:N0} sp");
                                table.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(5).AlignRight().Text($"{p.Price:N0} đ").Bold();
                            }
                        }
                    });

                    col.Item().PaddingTop(14).Row(row =>
                    {
                        row.RelativeItem().AlignCenter().Column(c =>
                        {
                            c.Item().Text("NGƯỜI LẬP BÁO CÁO").Bold().FontSize(9.5f);
                            c.Item().Text("(Ký và ghi rõ họ tên)").FontSize(8.5f).FontColor("#64748B");
                            c.Item().PaddingTop(34).Text(creatorName).Bold();
                        });
                        row.RelativeItem().AlignCenter().Column(c =>
                        {
                            c.Item().Text("QUẢN LÝ CỬA HÀNG").Bold().FontSize(9.5f);
                            c.Item().Text("(Xác nhận & đóng dấu)").FontSize(8.5f).FontColor("#64748B");
                        });
                    });
                });

                page.Footer().BorderTop(0.5f).BorderColor("#CBD5E1").PaddingTop(6).Row(row =>
                {
                    row.RelativeItem().Text("FashionHub • Hồ sơ báo cáo lưu trữ nội bộ").FontSize(8.5f).FontColor("#64748B");
                    row.RelativeItem().AlignRight().Text(x =>
                    {
                        x.Span("Trang ").FontSize(8.5f).FontColor("#64748B");
                        x.CurrentPageNumber().FontSize(8.5f).FontColor("#64748B");
                        x.Span(" / ").FontSize(8.5f).FontColor("#64748B");
                        x.TotalPages().FontSize(8.5f).FontColor("#64748B");
                    });
                });
            });
        }).GeneratePdf();

        var reportDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "reports");
        Directory.CreateDirectory(reportDir);

        var fileName = $"BaoCao_T{selectedMonth:00}_{selectedYear}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        var fullPath = Path.Combine(reportDir, fileName);
        await System.IO.File.WriteAllBytesAsync(fullPath, pdfBytes);

        return File(pdfBytes, "application/pdf", fileName);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteSavedReport(string fileName)
    {
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            var safeName = Path.GetFileName(fileName);
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "reports", safeName);
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
                TempData["ReportSuccess"] = $"Đã xóa bản lưu trữ báo cáo \"{safeName}\".";
            }
        }
        return RedirectToAction(nameof(Reports));
    }

    private async Task<SalesReportSnapshot> BuildReportSnapshotAsync(int selectedYear, int selectedMonth)
    {
        var monthStart = new DateTime(selectedYear, selectedMonth, 1);
        var monthEnd = monthStart.AddMonths(1);
        var prevMonthStart = monthStart.AddMonths(-1);

        var allMonthOrders = await _context.DonHangs
            .Where(d => d.ThoiGianTao >= monthStart && d.ThoiGianTao < monthEnd)
            .ToListAsync();

        var validMonthOrders = allMonthOrders.Where(d => d.TrangThai != "Da_huy").ToList();
        var prevValidOrders = await _context.DonHangs
            .Where(d => d.ThoiGianTao >= prevMonthStart && d.ThoiGianTao < monthStart && d.TrangThai != "Da_huy")
            .ToListAsync();

        var monthRevenue = validMonthOrders.Sum(d => d.TongThanhToan);
        var prevRevenue = prevValidOrders.Sum(d => d.TongThanhToan);
        decimal? growthPct = prevRevenue > 0
            ? Math.Round((monthRevenue - prevRevenue) * 100m / prevRevenue, 1)
            : null;

        var newCustomers = await _context.NguoiDungs
            .CountAsync(u => u.ThoiGianTao >= monthStart && u.ThoiGianTao < monthEnd && u.VaiTros.Any(v => v.MaVaiTro == "KHACH_HANG"));

        var yearOrders = await _context.DonHangs
            .Where(d => d.ThoiGianTao.Year == selectedYear && d.TrangThai != "Da_huy")
            .Select(d => new { d.ThoiGianTao.Month, d.TongThanhToan })
            .ToListAsync();

        var monthlyBreakdown = Enumerable.Range(1, 12).Select(m =>
        {
            var list = yearOrders.Where(o => o.Month == m).ToList();
            var rev = list.Sum(o => o.TongThanhToan);
            return new MonthlyRevenueRow
            {
                Month = m,
                OrderCount = list.Count,
                Revenue = rev,
                AvgOrder = list.Count > 0 ? rev / list.Count : 0m
            };
        }).ToList();

        var topProducts = await _context.DonHangChiTiets
            .Where(ct => ct.DonHang.ThoiGianTao.Year == selectedYear && ct.DonHang.TrangThai != "Da_huy")
            .GroupBy(ct => ct.TenSanPham)
            .Select(g => new ProductViewModel
            {
                Name = g.Key,
                Stock = g.Sum(x => x.SoLuong),
                Price = g.Sum(x => x.DonGia * x.SoLuong)
            })
            .OrderByDescending(x => x.Stock)
            .Take(5)
            .ToListAsync();

        var categorySales = await _context.DonHangChiTiets
            .Where(ct => ct.DonHang.ThoiGianTao.Year == selectedYear && ct.DonHang.TrangThai != "Da_huy")
            .GroupBy(ct => ct.BienThe.SanPham.DanhMuc.TenDanhMuc)
            .Select(g => new CategorySalesRow
            {
                CategoryName = g.Key,
                UnitsSold = g.Sum(x => x.SoLuong),
                Revenue = g.Sum(x => x.DonGia * x.SoLuong)
            })
            .OrderByDescending(x => x.Revenue)
            .ToListAsync();

        return new SalesReportSnapshot
        {
            ReportCode = $"BC-{selectedYear}{selectedMonth:00}-{DateTime.Now:HHmmss}",
            GeneratedAt = DateTime.Now,
            SelectedYear = selectedYear,
            SelectedMonth = selectedMonth,
            MonthRevenue = monthRevenue,
            PrevMonthRevenue = prevRevenue,
            RevenueGrowthPercent = growthPct,
            MonthOrders = validMonthOrders.Count,
            CompletedOrders = allMonthOrders.Count(d => d.TrangThai == "Da_giao"),
            ShippingOrders = allMonthOrders.Count(d => d.TrangThai == "Dang_giao"),
            PendingOrders = allMonthOrders.Count(d => d.TrangThai == "Cho_xac_nhan" || d.TrangThai == "Da_xac_nhan"),
            CancelledOrders = allMonthOrders.Count(d => d.TrangThai == "Da_huy"),
            NewCustomers = newCustomers,
            AvgOrder = validMonthOrders.Count > 0 ? monthRevenue / validMonthOrders.Count : 0m,
            YearRevenue = monthlyBreakdown.Sum(x => x.Revenue),
            YearOrders = monthlyBreakdown.Sum(x => x.OrderCount),
            MonthlyBreakdown = monthlyBreakdown,
            TopProducts = topProducts,
            CategorySales = categorySales
        };
    }

    private static List<SavedReportFileInfo> GetSavedReportFiles()
    {
        var reportDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "reports");
        if (!Directory.Exists(reportDir)) return new();

        return Directory.GetFiles(reportDir, "*.pdf")
            .Select(path => new FileInfo(path))
            .OrderByDescending(f => f.CreationTime)
            .Select(f => new SavedReportFileInfo
            {
                FileName = f.Name,
                Url = $"/reports/{f.Name}",
                CreatedAt = f.CreationTime,
                SizeKb = Math.Max(1, (long)Math.Round(f.Length / 1024.0))
            })
            .ToList();
    }

    // =========================================================================
    // 10. QUẢN LÝ KHU VỰC GIAO HÀNG - TỈNH / THÀNH PHỐ (PHẦN TỰ PHÁT TRIỂN)
    // =========================================================================

    
    public async Task<IActionResult> Payments(string? search, string? status)
{
    var query = _context.DonHangs
        .Where(o => o.PhuongThucThanhToan == "Chuyen_khoan"
                 || o.PhuongThucThanhToan == "ChuyenKhoan"
                 || o.PhuongThucThanhToan == "Bank")
        .OrderByDescending(o => o.ThoiGianTao)
        .AsQueryable();

    if (!string.IsNullOrWhiteSpace(search))
    {
        var kw = search.Trim().ToLower();
        query = query.Where(o =>
            (o.MaDonHang != null && o.MaDonHang.ToLower().Contains(kw)) ||
            (o.TenNguoiNhan != null && o.TenNguoiNhan.ToLower().Contains(kw)) ||
            (o.DienThoaiNhan != null && o.DienThoaiNhan.Contains(kw)));
    }

    if (!string.IsNullOrWhiteSpace(status))
    {
        var st = status.Trim();
        query = query.Where(o => o.TrangThaiThanhToan == st);
    }

    ViewBag.Search = search;
    ViewBag.Status = status;

    var orders = await query.ToListAsync();
    return View(orders);
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ConfirmPayment(long id)
{
    var order = await _context.DonHangs.FirstOrDefaultAsync(o => o.Id == id);
    if (order == null)
    {
        TempData["OrderError"] = "Không tìm thấy đơn hàng.";
        return RedirectToAction(nameof(Payments));
    }

    order.TrangThaiThanhToan = "Da_thanh_toan";
    order.ThoiGianCapNhat = DateTime.Now;
    await _context.SaveChangesAsync();

    TempData["OrderSuccess"] = $"Đã xác nhận thanh toán chuyển khoản cho đơn {order.MaDonHang}.";
    return RedirectToAction(nameof(Payments));
}
}

public class CategoryMetaInfo
{
    public string Icon { get; set; } = "👕";
    public string SizeType { get; set; } = "LETTER";
    public string Description { get; set; } = "";
}

public class ProductVariantInfo
{
    public long Id { get; set; }
    public string Sku { get; set; } = "";
    public int KichThuocId { get; set; }
    public string Size { get; set; } = "";
    public int MauSacId { get; set; }
    public string Color { get; set; } = "";
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int Reserved { get; set; }
    public int Available { get; set; }
    public bool TrangThai { get; set; }
}

public class SalesReportSnapshot
{
    public string ReportCode { get; set; } = "";
    public DateTime GeneratedAt { get; set; }
    public int SelectedYear { get; set; }
    public int SelectedMonth { get; set; }
    public decimal MonthRevenue { get; set; }
    public decimal PrevMonthRevenue { get; set; }
    public decimal? RevenueGrowthPercent { get; set; }
    public int MonthOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int ShippingOrders { get; set; }
    public int PendingOrders { get; set; }
    public int CancelledOrders { get; set; }
    public int NewCustomers { get; set; }
    public decimal AvgOrder { get; set; }
    public decimal YearRevenue { get; set; }
    public int YearOrders { get; set; }
    public List<MonthlyRevenueRow> MonthlyBreakdown { get; set; } = new();
    public List<ProductViewModel> TopProducts { get; set; } = new();
    public List<CategorySalesRow> CategorySales { get; set; } = new();
}

public class MonthlyRevenueRow
{
    public int Month { get; set; }
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
    public decimal AvgOrder { get; set; }
}

public class CategorySalesRow
{
    public string CategoryName { get; set; } = "";
    public int UnitsSold { get; set; }
    public decimal Revenue { get; set; }
}

public class SavedReportFileInfo
{
    public string FileName { get; set; } = "";
    public string Url { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public long SizeKb { get; set; }
}

public class ProductTrendAdvice
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string ProductCode { get; set; } = "";
    public string TrendType { get; set; } = "normal";
    public string TrendLabel { get; set; } = "";
    public string Summary { get; set; } = "";
    public decimal CurrentMargin { get; set; }
    public decimal SuggestedMargin { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal SuggestedPrice { get; set; }
    public int SuggestedDiscountPercent { get; set; }
    public string SuggestedPromoCode { get; set; } = "";
    public string ActionHint { get; set; } = "";
}