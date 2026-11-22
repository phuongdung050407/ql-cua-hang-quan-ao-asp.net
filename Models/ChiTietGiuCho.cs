using System;
using System.Collections.Generic;

namespace ShopProject.Models;

public partial class ChiTietGiuCho
{
    public long Id { get; set; }

    public long DonHangCtId { get; set; }

    public int KhoId { get; set; }

    public long BienTheId { get; set; }

    public int SoLuong { get; set; }

    public virtual DonHangChiTiet DonHangCt { get; set; } = null!;

    public virtual TonKho TonKho { get; set; } = null!;
}
