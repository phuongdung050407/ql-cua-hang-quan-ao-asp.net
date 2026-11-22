using System;
using System.Collections.Generic;

namespace ShopProject.Models;

public partial class DiaChi
{
    public long Id { get; set; }

    public long NguoiDungId { get; set; }

    public string TenNguoiNhan { get; set; } = null!;

    public string SoDienThoai { get; set; } = null!;

    public string DiaChiChiTiet { get; set; } = null!;

    public string? PhuongXa { get; set; }

    public string? QuanHuyen { get; set; }

    public string TinhThanh { get; set; } = null!;

    public bool MacDinh { get; set; }

    public DateTime ThoiGianTao { get; set; }

    public virtual NguoiDung NguoiDung { get; set; } = null!;
}
