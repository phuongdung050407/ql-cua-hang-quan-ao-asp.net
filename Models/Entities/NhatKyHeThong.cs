using System;
using System.Collections.Generic;

namespace ShopProject.Models.Entities;

public partial class NhatKyHeThong
{
    public long Id { get; set; }

    public long? NguoiDungId { get; set; }

    public string HanhDong { get; set; } = null!;

    public string DoiTuong { get; set; } = null!;

    public long? DoiTuongId { get; set; }

    public string? NoiDung { get; set; }

    public DateTime ThoiGian { get; set; }

    public virtual NguoiDung? NguoiDung { get; set; }
}
