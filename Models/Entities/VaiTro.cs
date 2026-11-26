using System;
using System.Collections.Generic;

namespace ShopProject.Models.Entities;

public partial class VaiTro
{
    public int Id { get; set; }

    public string MaVaiTro { get; set; } = null!;

    public string TenVaiTro { get; set; } = null!;

    public virtual ICollection<NguoiDung> NguoiDungs { get; set; } = new List<NguoiDung>();

    public virtual ICollection<Quyen> Quyens { get; set; } = new List<Quyen>();
}
