namespace ShopProject.Models;

public class DashboardViewModel
{
    public decimal RevenueToday { get; set; }
    public decimal RevenueMonth { get; set; }
    public int OrdersToday { get; set; }
    public int LowStockCount { get; set; }
    public int CustomerCount { get; set; }
    public List<ProductViewModel> Products { get; set; } = [];
    public List<OrderViewModel> RecentOrders { get; set; } = [];
}

public class ProductViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Status { get; set; } = "";
    public string Image { get; set; } = "";
}

public class OrderViewModel
{
    public string Code { get; set; } = "";
    public string Customer { get; set; } = "";
    public string Date { get; set; } = "";
    public decimal Total { get; set; }
    public string Status { get; set; } = "";
}
