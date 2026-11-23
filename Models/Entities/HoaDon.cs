using System;
using System.Collections.Generic;

namespace ShopProject.Models.Entities;

public partial class HoaDon
{
    public long Id { get; set; }

    public string MaHoaDon { get; set; } = null!;

    public long DonHangId { get; set; }

    public DateTime NgayLap { get; set; }

    public decimal TongTien { get; set; }

    public decimal Thue { get; set; }

    public virtual DonHang DonHang { get; set; } = null!;
}
