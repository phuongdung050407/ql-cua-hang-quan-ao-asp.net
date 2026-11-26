using System;
using System.Collections.Generic;

namespace ShopProject.Models.Entities;

public partial class ChatLieu
{
    public int Id { get; set; }

    public string TenChatLieu { get; set; } = null!;

    public virtual ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();
}
