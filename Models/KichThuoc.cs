using System;
using System.Collections.Generic;

namespace ShopProject.Models;

public partial class KichThuoc
{
    public int Id { get; set; }

    public string MaSize { get; set; } = null!;

    public int ThuTu { get; set; }

    public virtual ICollection<BienTheSanPham> BienTheSanPhams { get; set; } = new List<BienTheSanPham>();
}
