using System;
using System.Collections.Generic;

namespace ShopProject.Models.Entities;

public partial class XacThucTaiKhoan
{
    public long Id { get; set; }

    public long NguoiDungId { get; set; }

    public string MaXacThucHash { get; set; } = null!;

    public string MucDich { get; set; } = null!;

    public DateTime HetHan { get; set; }

    public bool DaSuDung { get; set; }

    public DateTime ThoiGianTao { get; set; }

    public virtual NguoiDung NguoiDung { get; set; } = null!;
}
