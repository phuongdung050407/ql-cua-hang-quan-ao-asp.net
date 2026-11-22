namespace ShopProject.Models;

public class CategoryViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int ProductCount { get; set; }
    public string Status { get; set; } = "Hoạt động";
}

public class PersonViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Role { get; set; } = "";
    public string Status { get; set; } = "Hoạt động";
}

public class InventoryViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Image { get; set; } = "";
    public string Sku { get; set; } = "";
    public int Stock { get; set; }
    public int Reserved { get; set; }
    public string Location { get; set; } = "";
}

public class PromotionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string Discount { get; set; } = "";
    public string Period { get; set; } = "";
    public string Status { get; set; } = "";
}

public class ReviewViewModel
{
    public int Id { get; set; }
    public string Customer { get; set; } = "";
    public string Product { get; set; } = "";
    public int Rating { get; set; }
    public string Comment { get; set; } = "";
    public string Date { get; set; } = "";
    public string Status { get; set; } = "Chờ duyệt";
}

public class StoreViewModel
{
    public List<ProductViewModel> Products { get; set; } = [];
    public List<CategoryViewModel> Categories { get; set; } = [];
}
