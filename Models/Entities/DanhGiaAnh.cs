using System;
using System.Collections.Generic;

namespace ShopProject.Models.Entities;

public partial class DanhGiaAnh
{
    public long Id { get; set; }

    public long DanhGiaId { get; set; }

    public string Url { get; set; } = null!;

    public virtual DanhGium DanhGia { get; set; } = null!;
}
