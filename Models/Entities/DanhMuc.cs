using System;
using System.Collections.Generic;

namespace ShopProject.Models.Entities;

public partial class DanhMuc
{
    public int Id { get; set; }

    public string TenDanhMuc { get; set; } = null!;

    public string? MoTa { get; set; }

    public bool TrangThai { get; set; }

    public DateTime ThoiGianTao { get; set; }

    public virtual ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();
}
