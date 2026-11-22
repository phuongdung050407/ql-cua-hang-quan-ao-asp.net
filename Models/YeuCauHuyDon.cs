using System;
using System.Collections.Generic;

namespace ShopProject.Models;

public partial class YeuCauHuyDon
{
    public long Id { get; set; }

    public long DonHangId { get; set; }

    public long NguoiDungId { get; set; }

    public string LyDo { get; set; } = null!;

    public string TrangThai { get; set; } = null!;

    public long? XuLyBoi { get; set; }

    public DateTime ThoiGianTao { get; set; }

    public DateTime? ThoiGianXuLy { get; set; }

    public virtual DonHang DonHang { get; set; } = null!;

    public virtual NguoiDung NguoiDung { get; set; } = null!;

    public virtual NguoiDung? XuLyBoiNavigation { get; set; }
}
