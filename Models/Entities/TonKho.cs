using System;
using System.Collections.Generic;

namespace ShopProject.Models.Entities;

public partial class TonKho
{
    public long Id { get; set; }

    public int KhoId { get; set; }

    public long BienTheId { get; set; }

    public int SoLuongTon { get; set; }

    public int SoLuongGiuCho { get; set; }

    public virtual BienTheSanPham BienThe { get; set; } = null!;

    public virtual ICollection<ChiTietGiuCho> ChiTietGiuChos { get; set; } = new List<ChiTietGiuCho>();

    public virtual Kho Kho { get; set; } = null!;
}
