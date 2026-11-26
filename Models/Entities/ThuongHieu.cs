using System;
using System.Collections.Generic;

namespace ShopProject.Models.Entities;

public partial class ThuongHieu
{
    public int Id { get; set; }

    public string TenThuongHieu { get; set; } = null!;

    public string? MoTa { get; set; }

    public virtual ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();
}
