using System;
using System.Collections.Generic;

namespace ShopProject.Models;

public partial class GioHang
{
    public long Id { get; set; }

    public long NguoiDungId { get; set; }

    public DateTime ThoiGianTao { get; set; }

    public virtual ICollection<GioHangChiTiet> GioHangChiTiets { get; set; } = new List<GioHangChiTiet>();

    public virtual NguoiDung NguoiDung { get; set; } = null!;
}
