using System;
using System.Collections.Generic;

namespace ShopProject.Models.Entities;

public partial class LichSuXemSanPham
{
    public long Id { get; set; }

    public long NguoiDungId { get; set; }

    public int SanPhamId { get; set; }

    public DateTime ThoiGianXem { get; set; }

    public virtual NguoiDung NguoiDung { get; set; } = null!;

    public virtual SanPham SanPham { get; set; } = null!;
}
