using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ShopProject.Models;
using ShopProject.Models.Entities;

namespace ShopProject.Controllers;

[Authorize(Roles = "Admin,Employee")]
public class ManagementController : Controller
{
    private readonly ClothingShopDbContext _context;

    public ManagementController(ClothingShopDbContext context)
    {
        _context = context;
    }

    // =========================================================================
    // 1. QUẢN LÝ ĐƠN ĐẶT HÀNG (GỌI sp_CapNhatTrangThaiDon)
    // =========================================================================

    public async Task<IActionResult> Orders(string? search, string? status)
    {
        var firstDayOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        ViewBag.PendingCount = await _context.DonHangs.CountAsync(d => d.TrangThai == "Cho_xac_nhan" || d.TrangThai == "Da_xac_nhan");
        ViewBag.ShippingCount = await _context.DonHangs.CountAsync(d => d.TrangThai == "Dang_giao");
        ViewBag.CompletedCount = await _context.DonHangs.CountAsync(d => d.TrangThai == "Da_giao");
        ViewBag.MonthCount = await _context.DonHangs.CountAsync(d => d.ThoiGianTao >= firstDayOfMonth);
        ViewBag.Search = search;
        ViewBag.Status = status;

        var query = _context.DonHangs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var kw = search.Trim();
            query = query.Where(d => d.MaDonHang.Contains(kw) || d.TenNguoiNhan.Contains(kw) || d.DienThoaiNhan.Contains(kw));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(d => d.TrangThai == status);
        }

        var orders = await query
            .OrderByDescending(d => d.ThoiGianTao)
            .ToListAsync();

        return View(orders);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrderStatus(long id, string status)
    {
        long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);

        var pId = new SqlParameter("@DonHangID", System.Data.SqlDbType.BigInt) { Value = id };
        var pStatus = new SqlParameter("@TrangThaiMoi", System.Data.SqlDbType.VarChar, 30) { Value = status };
        var pUser = new SqlParameter("@NguoiThucHien", System.Data.SqlDbType.BigInt) { Value = userId > 0 ? userId : DBNull.Value };
        var pNote = new SqlParameter("@GhiChu", System.Data.SqlDbType.NVarChar, 300) { Value = "Cập nhật từ trang quản trị" };

        try
        {
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_CapNhatTrangThaiDon @DonHangID, @TrangThaiMoi, @NguoiThucHien, @GhiChu",
                pId, pStatus, pUser, pNote);
            TempData["OrderSuccess"] = "Đã cập nhật trạng thái đơn hàng thành công.";
        }
        catch (SqlException ex)
        {
            TempData["OrderError"] = ex.Message;
        }

        return RedirectToAction(nameof(Orders));
    }

    // =========================================================================
    // 2. QUẢN LÝ KHO HÀNG (CẢNH BÁO TỒN KHO & NHẬP EXCEL QUA CKFINDER)
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
    public async Task<IActionResult> ImportStock(int khoId, int bienTheId, int soLuongNhap)
    {
        if (soLuongNhap <= 0)
        {
            TempData["InventoryError"] = "Số lượng nhập kho phải lớn hơn 0.";
            return RedirectToAction(nameof(Inventory));
        }

        var tk = await _context.TonKhos
            .Include(t => t.BienThe)
            .FirstOrDefaultAsync(t => t.KhoId == khoId && t.BienTheId == bienTheId);

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

        await _context.SaveChangesAsync();
        TempData["InventorySuccess"] = $"Đã nhập thêm +{soLuongNhap} sản phẩm vào kho thành công.";
        return RedirectToAction(nameof(Inventory));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustStock(int khoId, int bienTheId, int soLuongMoi)
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
    [RequestSizeLimit(5 * 1024 * 1024)] // Giới hạn tối đa 5 MB
    public async Task<IActionResult> ImportStockExcel(IFormFile? excelFile)
    {
        const long maxFileBytes = 5 * 1024 * 1024; // 5 MB

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
    // 3. QUẢN LÝ DANH MỤC SẢN PHẨM
    // =========================================================================

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Categories()
    {
        var danhMucs = await _context.DanhMucs
            .Include(dm => dm.SanPhams)
            .OrderBy(dm => dm.Id)
            .ToListAsync();

        ViewBag.CategoryMeta = danhMucs.ToDictionary(
            dm => dm.Id,
            dm =>
            {
                var rawDesc = dm.MoTa ?? "";
                var icon = "";
                var cleanDesc = rawDesc;

                if (rawDesc.StartsWith("[ICON:") && rawDesc.Contains(']'))
                {
                    var endIdx = rawDesc.IndexOf(']');
                    icon = rawDesc.Substring(6, endIdx - 6);
                    cleanDesc = rawDesc.Substring(endIdx + 1).Trim();
                }
                else
                {
                    var lower = dm.TenDanhMuc.ToLowerInvariant();
                    icon = lower.Contains("quần") ? "👖"
                         : (lower.Contains("váy") || lower.Contains("đầm")) ? "👗"
                         : lower.Contains("khoác") ? "🧥"
                         : lower.Contains("sơ mi") ? "👔"
                         : lower.Contains("áo") ? "👕"
                         : (lower.Contains("giày") || lower.Contains("dép")) ? "👟"
                         : (lower.Contains("túi") || lower.Contains("phụ kiện") || lower.Contains("mũ")) ? "👜"
                         : "◫";
                }

                return new { Icon = icon, Description = cleanDesc };
            });

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
    public async Task<IActionResult> CreateCategory(string tenDanhMuc, string? icon, string? moTa)
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

        var chosenIcon = string.IsNullOrWhiteSpace(icon) ? "👕" : icon.Trim();
        var cleanMoTa = (moTa ?? "").Trim();

        _context.DanhMucs.Add(new DanhMuc
        {
            TenDanhMuc = ten,
            MoTa = string.IsNullOrEmpty(cleanMoTa) ? $"[ICON:{chosenIcon}]" : $"[ICON:{chosenIcon}] {cleanMoTa}",
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
    public async Task<IActionResult> EditCategory(int id, string tenDanhMuc, string? icon, string? moTa)
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

        var chosenIcon = string.IsNullOrWhiteSpace(icon) ? "👕" : icon.Trim();
        var cleanMoTa = (moTa ?? "").Trim();

        dm.TenDanhMuc = ten;
        dm.MoTa = string.IsNullOrEmpty(cleanMoTa) ? $"[ICON:{chosenIcon}]" : $"[ICON:{chosenIcon}] {cleanMoTa}";
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
    // 4. QUẢN LÝ SẢN PHẨM, TỰ ĐỘNG TÍNH LỢI NHUẬN & GỢI Ý XU HƯỚNG
    // =========================================================================

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Products(string? search, int? categoryId, string? status)
    {
        ViewBag.Categories = await _context.DanhMucs.Where(d => d.TrangThai).ToListAsync();
        ViewBag.Sizes = await _context.KichThuocs.OrderBy(k => k.ThuTu).ToListAsync();
        ViewBag.Colors = await _context.MauSacs.ToListAsync();
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

        // Thống kê lượng bán 30 ngày gần nhất theo sản phẩm để phân tích xu hướng
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

        // Tạo gợi ý điều chỉnh % lợi nhuận & chương trình giảm giá theo xu hướng bán hàng
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
                // Xu hướng BÁN CHẠY -> Gợi ý tăng biên lợi nhuận thêm +5% hoặc +8%
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
                // Xu hướng BÁN CHẬM / TỒN NHIỀU -> Gợi ý hạ biên lợi nhuận & tạo mã giảm giá kích cầu
                var safeFloorPrice = Math.Round(cost * 1.05m / 1000m) * 1000m; // Giữ tối thiểu 5% lãi trên vốn
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
        int mauSacId,
        int soLuongTon,
        string? imageUrl,
        string? moTa)
    {
        if (string.IsNullOrWhiteSpace(tenSanPham) || giaGoc <= 0)
        {
            TempData["ProductError"] = "Vui lòng nhập tên sản phẩm và giá gốc hợp lệ (> 0 đ).";
            return RedirectToAction(nameof(Products));
        }

        // Tự động tính giá bán theo % lợi nhuận nếu không nhập đè thủ công
        var computedPrice = Math.Round(giaGoc * (1 + Math.Max(0, tyLeLoiNhuan) / 100m) / 1000m) * 1000m;
        var finalPrice = (giaBan.HasValue && giaBan.Value > 0) ? giaBan.Value : computedPrice;

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
            Sku = $"{maSp}-S{kichThuocId}-M{mauSacId}",
            KichThuocId = kichThuocId,
            MauSacId = mauSacId,
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
        var profitPerUnit = finalPrice - giaGoc;
        TempData["ProductSuccess"] = $"Đã tạo \"{sp.TenSanPham}\" — Giá gốc: {giaGoc:N0} đ | Giá bán: {finalPrice:N0} đ (Lãi +{profitPerUnit:N0} đ/sp).";
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

        var computedPrice = Math.Round(giaGoc * (1 + Math.Max(0, tyLeLoiNhuan) / 100m) / 1000m) * 1000m;
        var finalPrice = (giaBan.HasValue && giaBan.Value > 0) ? giaBan.Value : computedPrice;

        sp.TenSanPham = tenSanPham.Trim();
        sp.DanhMucId = danhMucId;
        sp.GiaGoc = giaGoc;
        sp.GiaBan = finalPrice;
        sp.MoTa = moTa?.Trim();
        sp.ThoiGianCapNhat = DateTime.Now;

        // Đồng bộ giá bán cho các phân loại đang dùng giá cơ bản cũ
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
        TempData["ProductSuccess"] = $"Đã cập nhật \"{sp.TenSanPham}\" — Giá bán mới: {finalPrice:N0} đ (Lãi +{(finalPrice - giaGoc):N0} đ/sp).";
        return RedirectToAction(nameof(Products));
    }

    // Áp dụng nhanh gợi ý % lợi nhuận theo xu hướng bán hàng
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
        var newPrice = Math.Round(cost * (1 + Math.Max(0, newMarginPercent) / 100m) / 1000m) * 1000m;

        sp.GiaGoc = cost;
        sp.GiaBan = newPrice;
        sp.ThoiGianCapNhat = DateTime.Now;
        foreach (var bt in sp.BienTheSanPhams)
        {
            bt.GiaBan = newPrice;
        }

        await _context.SaveChangesAsync();
        TempData["ProductSuccess"] = $"Đã điều chỉnh lợi nhuận \"{sp.TenSanPham}\" thành {newMarginPercent:0.#}% → Giá bán mới: {newPrice:N0} đ.";
        return RedirectToAction(nameof(Products));
    }
    // =========================================================================
    // 5. QUẢN LÝ NGƯỜI DÙNG
    // =========================================================================

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Users(string? search, string? role)
    {
        ViewBag.Search = search;
        ViewBag.SelectedRole = role;

        var query = _context.NguoiDungs
            .Include(u => u.VaiTros)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var kw = search.Trim();
            query = query.Where(u => u.HoTen.Contains(kw) || u.Email.Contains(kw) || (u.SoDienThoai != null && u.SoDienThoai.Contains(kw)));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(u => u.VaiTros.Any(v => v.MaVaiTro == role));
        }

        var users = await query
            .OrderByDescending(u => u.ThoiGianTao)
            .ToListAsync();

        var items = users.Select(u => new PersonViewModel
        {
            Id = (int)u.Id,
            Name = u.HoTen,
            Email = u.Email,
            Phone = u.SoDienThoai ?? "Chưa cập nhật",
            Role = u.VaiTros.Select(v => v.TenVaiTro).FirstOrDefault() ?? "Khách hàng",
            Status = u.TrangThai == "Hoat_dong" ? "Hoạt động" : "Tạm khóa"
        }).ToList();

        return View(items);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateUserStatus(long id, string status)
    {
        var user = await _context.NguoiDungs.FindAsync(id);
        if (user != null)
        {
            user.TrangThai = status == "Khoa" ? "Khoa" : "Hoat_dong";
            user.ThoiGianCapNhat = DateTime.Now;
            await _context.SaveChangesAsync();
            TempData["UserSuccess"] = "Đã cập nhật trạng thái tài khoản.";
        }
        return RedirectToAction(nameof(Users));
    }

    // =========================================================================
    // 6. QUẢN LÝ NHÂN VIÊN
    // =========================================================================

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Employees()
    {
        var nvs = await _context.NhanViens
            .Include(nv => nv.NguoiDung)
            .OrderByDescending(nv => nv.Id)
            .ToListAsync();

        ViewBag.TotalEmployees = nvs.Count;
        ViewBag.WorkingCount = nvs.Count(n => n.TrangThai == "Dang_lam");
        ViewBag.LeaveCount = nvs.Count(n => n.TrangThai == "Tam_nghi");
        ViewBag.ManagerCount = nvs.Count(n => n.ChucVu.Contains("Quản lý") || n.ChucVu.Contains("Quản trị"));

        var items = nvs.Select(nv => new PersonViewModel
        {
            Id = (int)nv.Id,
            Name = nv.NguoiDung.HoTen,
            Email = nv.NguoiDung.Email,
            Phone = nv.NguoiDung.SoDienThoai ?? "",
            Role = $"{nv.ChucVu} ({nv.MaNhanVien})",
            Status = nv.TrangThai switch
            {
                "Dang_lam" => "Hoạt động",
                "Tam_nghi" => "Nghỉ phép",
                _ => "Tạm khóa"
            }
        }).ToList();

        return View(items);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEmployee(string hoTen, string email, string phone, string password, string chucVu, decimal luong)
    {
        var emailChuan = (email ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(hoTen) || string.IsNullOrWhiteSpace(emailChuan) || string.IsNullOrWhiteSpace(password))
        {
            TempData["EmployeeError"] = "Vui lòng nhập đầy đủ họ tên, email và mật khẩu.";
            return RedirectToAction(nameof(Employees));
        }

        if (await _context.NguoiDungs.AnyAsync(u => u.EmailChuan == emailChuan))
        {
            TempData["EmployeeError"] = "Email này đã tồn tại trong hệ thống.";
            return RedirectToAction(nameof(Employees));
        }

        var user = new NguoiDung
        {
            Email = email!.Trim(),
            HoTen = hoTen.Trim(),
            SoDienThoai = phone?.Trim(),
            MatKhauHash = BCrypt.Net.BCrypt.HashPassword(password),
            TrangThai = "Hoat_dong",
            ThoiGianTao = DateTime.Now,
            ThoiGianCapNhat = DateTime.Now
        };

        var roleNv = await _context.VaiTros.FirstOrDefaultAsync(v => v.MaVaiTro == "NHAN_VIEN");
        if (roleNv != null) user.VaiTros.Add(roleNv);

        _context.NguoiDungs.Add(user);
        await _context.SaveChangesAsync();

        _context.NhanViens.Add(new NhanVien
        {
            NguoiDungId = user.Id,
            MaNhanVien = "NV-" + DateTime.Now.ToString("yyMMddHHmm"),
            ChucVu = string.IsNullOrWhiteSpace(chucVu) ? "Nhân viên bán hàng" : chucVu.Trim(),
            NgayVaoLam = DateOnly.FromDateTime(DateTime.Today),
            Luong = Math.Max(0, luong),
            TrangThai = "Dang_lam"
        });
        await _context.SaveChangesAsync();

        TempData["EmployeeSuccess"] = "Đã thêm nhân viên mới thành công.";
        return RedirectToAction(nameof(Employees));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateEmployeeStatus(long id, string status)
    {
        var nv = await _context.NhanViens.Include(x => x.NguoiDung).FirstOrDefaultAsync(x => x.Id == id);
        if (nv != null)
        {
            nv.TrangThai = status;
            nv.NguoiDung.TrangThai = status == "Da_nghi" ? "Khoa" : "Hoat_dong";
            await _context.SaveChangesAsync();
            TempData["EmployeeSuccess"] = "Đã cập nhật trạng thái nhân viên.";
        }
        return RedirectToAction(nameof(Employees));
    }

    // =========================================================================
    // 7. QUẢN LÝ KHUYẾN MÃI (VOUCHER)
    // =========================================================================

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Promotions()
    {
        var now = DateTime.Now;
        var vouchers = await _context.MaGiamGia
            .OrderByDescending(v => v.Id)
            .ToListAsync();

        ViewBag.ActiveCount = vouchers.Count(v => v.TrangThai && v.NgayBatDau <= now && v.NgayKetThuc >= now);
        ViewBag.UsedCount = vouchers.Sum(v => v.SoLuongDaDung);
        ViewBag.EndingSoonCount = vouchers.Count(v => v.TrangThai && v.NgayKetThuc >= now && v.NgayKetThuc <= now.AddDays(7));

        var items = vouchers.Select(v =>
        {
            var discountText = v.LoaiGiam == "Phan_tram"
                ? $"GIẢM {v.GiaTri:0}%"
                : $"GIẢM {v.GiaTri:N0}đ";

            var statusText = !v.TrangThai
                ? "Tạm dừng"
                : (v.NgayKetThuc < now ? "Đã hết hạn" : (v.NgayBatDau > now ? "Sắp diễn ra" : "Đang chạy"));

            return new PromotionViewModel
            {
                Id = v.Id,
                Name = string.IsNullOrWhiteSpace(v.MoTa) ? $"Mã ưu đãi {v.MaVoucher}" : v.MoTa,
                Code = v.MaVoucher,
                Discount = discountText,
                Period = $"{v.NgayBatDau:dd/MM/yyyy} - {v.NgayKetThuc:dd/MM/yyyy} (Đã dùng {v.SoLuongDaDung}/{v.SoLuongPhatHanh})",
                Status = statusText
            };
        }).ToList();

        return View(items);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePromotion(
        string maVoucher,
        string? moTa,
        string loaiGiam,
        decimal giaTri,
        decimal donToiThieu,
        int soLuongPhatHanh,
        DateTime ngayBatDau,
        DateTime ngayKetThuc)
    {
        var code = (maVoucher ?? "").Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code) || giaTri <= 0 || soLuongPhatHanh <= 0 || ngayKetThuc <= ngayBatDau)
        {
            TempData["PromoError"] = "Thông tin mã khuyến mãi không hợp lệ (kiểm tra mã, giá trị và ngày kết thúc phải sau ngày bắt đầu).";
            return RedirectToAction(nameof(Promotions));
        }

        if (await _context.MaGiamGia.AnyAsync(v => v.MaVoucher == code))
        {
            TempData["PromoError"] = "Mã voucher này đã tồn tại.";
            return RedirectToAction(nameof(Promotions));
        }

        _context.MaGiamGia.Add(new MaGiamGium
        {
            MaVoucher = code,
            MoTa = moTa?.Trim(),
            LoaiGiam = loaiGiam == "Tien_mat" ? "Tien_mat" : "Phan_tram",
            GiaTri = giaTri,
            DonToiThieu = Math.Max(0, donToiThieu),
            SoLuongPhatHanh = soLuongPhatHanh,
            SoLuongDaDung = 0,
            NgayBatDau = ngayBatDau,
            NgayKetThuc = ngayKetThuc,
            TrangThai = true
        });
        await _context.SaveChangesAsync();

        TempData["PromoSuccess"] = $"Đã tạo mã khuyến mãi {code} thành công.";
        return RedirectToAction(nameof(Promotions));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePromotion(int id)
    {
        var voucher = await _context.MaGiamGia.FindAsync(id);
        if (voucher != null)
        {
            voucher.TrangThai = !voucher.TrangThai;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Promotions));
    }

    // =========================================================================
    // 8. QUẢN LÝ ĐÁNH GIÁ SẢN PHẨM
    // =========================================================================

    public async Task<IActionResult> Reviews()
    {
        var list = await _context.DanhGia
            .Include(dg => dg.NguoiDung)
            .Include(dg => dg.SanPham)
            .OrderByDescending(dg => dg.ThoiGianTao)
            .ToListAsync();

        ViewBag.TotalReviews = list.Count;
        ViewBag.PendingReviews = list.Count(r => r.TrangThai == "Cho_duyet");
        ViewBag.AvgRating = list.Count > 0 ? list.Average(r => (double)r.SoSao) : 5.0;
        ViewBag.PositiveRate = list.Count > 0 ? (int)Math.Round(list.Count(r => r.SoSao >= 4) * 100.0 / list.Count) : 100;

        var items = list.Select(dg => new ReviewViewModel
        {
            Id = (int)dg.Id,
            Customer = dg.NguoiDung.HoTen,
            Product = dg.SanPham.TenSanPham,
            Rating = dg.SoSao,
            Comment = string.IsNullOrWhiteSpace(dg.PhanHoiShop)
                ? (dg.NoiDung ?? "")
                : $"{dg.NoiDung} — [Shop phản hồi: {dg.PhanHoiShop}]",
            Date = dg.ThoiGianTao.ToString("dd/MM/yyyy"),
            Status = dg.TrangThai switch
            {
                "Da_duyet" => "Đã duyệt",
                "An" => "Đã ẩn",
                _ => "Chờ duyệt"
            }
        }).ToList();

        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateReview(long id, string actionType, string? replyText)
    {
        var dg = await _context.DanhGia.FindAsync(id);
        if (dg != null)
        {
            if (actionType == "approve")
            {
                dg.TrangThai = "Da_duyet";
            }
            else if (actionType == "hide")
            {
                dg.TrangThai = "An";
            }
            else if (actionType == "reply" && !string.IsNullOrWhiteSpace(replyText))
            {
                dg.PhanHoiShop = replyText.Trim();
                dg.ThoiGianPhanHoi = DateTime.Now;
                dg.TrangThai = "Da_duyet";
            }

            await _context.SaveChangesAsync();
            TempData["ReviewSuccess"] = "Đã cập nhật đánh giá thành công.";
        }
        return RedirectToAction(nameof(Reviews));
    }

    // =========================================================================
    // 9. BÁO CÁO & THỐNG KÊ DOANH THU + XUẤT & LƯU TRỮ FILE PDF
    // =========================================================================

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reports(int? year, int? month)
    {
        var selectedYear = year ?? DateTime.Today.Year;
        var selectedMonth = (month is >= 1 and <= 12) ? month.Value : DateTime.Today.Month;

        var data = await BuildReportSnapshotAsync(selectedYear, selectedMonth);

        ViewBag.SelectedYear = selectedYear;
        ViewBag.SelectedMonth = selectedMonth;
        ViewBag.ReportData = data;
        ViewBag.SavedReports = GetSavedReportFiles();

        return View();
    }

    // Xem bản chứng từ báo cáo khổ A4 trên web (tương tự trang Hóa đơn)
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReportDocument(int? year, int? month)
    {
        var selectedYear = year ?? DateTime.Today.Year;
        var selectedMonth = (month is >= 1 and <= 12) ? month.Value : DateTime.Today.Month;

        var data = await BuildReportSnapshotAsync(selectedYear, selectedMonth);
        return View(data);
    }

    // Xuất file PDF thật, lưu vào wwwroot/reports/ và trả file tải về máy
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

                // Tiêu đề chứng từ báo cáo
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
                        titleCol.Item().Text($"BÁO CÁO TỔNG HỢP DOANH THU & BÁN HÀNG").FontSize(15).Bold().FontColor("#0F172A");
                        titleCol.Item().Text($"Kỳ báo cáo: Tháng {selectedMonth:00}/{selectedYear} (Lũy kế năm {selectedYear})").FontSize(10.5f).FontColor("#475569");
                    });
                });

                // Nội dung chính
                page.Content().PaddingVertical(14).Column(col =>
                {
                    col.Spacing(14);

                    // 1. Khối 4 chỉ số KPI tháng
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
                        KpiBox("KHÁCH HÀNG MỚI", $"{data.NewCustomers} khách", $"Tài khoản đăng ký mới");
                        KpiBox("GIÁ TRỊ ĐƠN TB", $"{data.AvgOrder:N0} đ", $"Trung bình / đơn hợp lệ");
                    });

                    // 2. Bảng chi tiết doanh thu 12 tháng
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

                    // 3. Top sản phẩm bán chạy
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

                    // 4. Chữ ký xác nhận
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

        // Lưu trữ bản sao PDF vào wwwroot/reports/
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
    // 10. QUẢN LÝ KHU VỰC GIAO HÀNG (TỈNH / THÀNH PHỐ)
    // =========================================================================

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Provinces()
    {
        var items = await _context.TinhThanhs
            .OrderByDescending(t => t.TrangThai)
            .ThenBy(t => t.TenTinhThanh)
            .ToListAsync();
        return View(items);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProvince(string tenTinhThanh)
    {
        if (string.IsNullOrWhiteSpace(tenTinhThanh))
        {
            TempData["ProvinceError"] = "Tên tỉnh / thành phố không được để trống.";
            return RedirectToAction(nameof(Provinces));
        }

        var ten = tenTinhThanh.Trim();
        if (await _context.TinhThanhs.AnyAsync(t => t.TenTinhThanh == ten))
        {
            TempData["ProvinceError"] = "Tỉnh / thành phố này đã tồn tại trong danh sách.";
            return RedirectToAction(nameof(Provinces));
        }

        _context.TinhThanhs.Add(new TinhThanh
        {
            TenTinhThanh = ten,
            TrangThai = true
        });
        await _context.SaveChangesAsync();

        TempData["ProvinceSuccess"] = $"Đã thêm khu vực giao hàng: {ten}.";
        return RedirectToAction(nameof(Provinces));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProvince(int id, string tenTinhThanh)
    {
        var item = await _context.TinhThanhs.FindAsync(id);
        if (item == null || string.IsNullOrWhiteSpace(tenTinhThanh))
        {
            TempData["ProvinceError"] = "Tên tỉnh / thành phố không hợp lệ.";
            return RedirectToAction(nameof(Provinces));
        }

        var ten = tenTinhThanh.Trim();
        if (await _context.TinhThanhs.AnyAsync(t => t.TenTinhThanh == ten && t.Id != id))
        {
            TempData["ProvinceError"] = "Tỉnh / thành phố này đã tồn tại trong danh sách.";
            return RedirectToAction(nameof(Provinces));
        }

        item.TenTinhThanh = ten;
        await _context.SaveChangesAsync();

        TempData["ProvinceSuccess"] = $"Đã cập nhật khu vực thành \"{ten}\".";
        return RedirectToAction(nameof(Provinces));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleProvince(int id)
    {
        var item = await _context.TinhThanhs.FindAsync(id);
        if (item != null)
        {
            item.TrangThai = !item.TrangThai;
            await _context.SaveChangesAsync();
            TempData["ProvinceSuccess"] = $"Đã {(item.TrangThai ? "mở lại" : "tạm ngưng")} giao hàng tại {item.TenTinhThanh}.";
        }
        return RedirectToAction(nameof(Provinces));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProvince(int id)
    {
        var item = await _context.TinhThanhs.FindAsync(id);
        if (item != null)
        {
            _context.TinhThanhs.Remove(item);
            await _context.SaveChangesAsync();
            TempData["ProvinceSuccess"] = $"Đã xóa khu vực \"{item.TenTinhThanh}\" khỏi danh sách.";
        }
        return RedirectToAction(nameof(Provinces));
    }
   
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
    public string TrendType { get; set; } = "normal"; // hot | slow | normal
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