using System;
using System.Collections.Generic;

namespace ShopProject.Models.Entities;

public partial class ThanhToan
{
    public long Id { get; set; }

    public long DonHangId { get; set; }

    public string? MaGiaoDich { get; set; }

    public string PhuongThuc { get; set; } = null!;

    public decimal SoTien { get; set; }

    public string TrangThai { get; set; } = null!;

    public DateTime? ThoiGianGiaoDich { get; set; }

    public DateTime ThoiGianTao { get; set; }

    public virtual DonHang DonHang { get; set; } = null!;
}
