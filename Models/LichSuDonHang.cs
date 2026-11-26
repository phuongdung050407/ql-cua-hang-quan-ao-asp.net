using System;
using System.Collections.Generic;

namespace ShopProject.Models;

public partial class LichSuDonHang
{
    public long Id { get; set; }

    public long DonHangId { get; set; }

    public string? TrangThaiCu { get; set; }

    public string TrangThaiMoi { get; set; } = null!;

    public string? GhiChu { get; set; }

    public long? NguoiCapNhat { get; set; }

    public DateTime ThoiGian { get; set; }

    public virtual DonHang DonHang { get; set; } = null!;

    public virtual NguoiDung? NguoiCapNhatNavigation { get; set; }
}
