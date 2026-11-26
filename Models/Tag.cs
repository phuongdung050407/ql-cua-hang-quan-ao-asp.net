using System;
using System.Collections.Generic;

namespace ShopProject.Models;

public partial class Tag
{
    public int Id { get; set; }

    public string TenTag { get; set; } = null!;

    public string Loai { get; set; } = null!;

    public virtual ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();
}
