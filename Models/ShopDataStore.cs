namespace ShopProject.Models;

public static class ShopDataStore
{
    public static List<ProductViewModel> Products { get; } =
    [
        new() { Id=1, Name="Áo sơ mi linen trắng", Category="Áo sơ mi", Price=450000, Stock=20, Status="Đang bán", Image="https://images.unsplash.com/photo-1602810318383-e386cc2a3ccf?w=600" },
        new() { Id=2, Name="Váy hoa midi", Category="Váy đầm", Price=680000, Stock=14, Status="Đang bán", Image="https://images.unsplash.com/photo-1595777457583-95e059d581b8?w=600" },
        new() { Id=3, Name="Quần jean nữ slim", Category="Quần", Price=550000, Stock=9, Status="Đang bán", Image="https://images.unsplash.com/photo-1542272604-787c3835535d?w=600" },
        new() { Id=4, Name="Áo khoác denim", Category="Áo khoác", Price=890000, Stock=7, Status="Đang bán", Image="https://images.unsplash.com/photo-1523205771623-e0faa4d2813d?w=600" },
        new() { Id=5, Name="Đầm suông mùa hè", Category="Váy đầm", Price=720000, Stock=11, Status="Đang bán", Image="https://images.unsplash.com/photo-1566174053879-31528523f8ae?w=600" },
        new() { Id=6, Name="Áo thun basic", Category="Áo thun", Price=290000, Stock=25, Status="Đang bán", Image="https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=600" }
    ];

    public static List<CategoryViewModel> Categories { get; } =
    [
        new() { Id=1, Name="Áo sơ mi", ProductCount=18 },
        new() { Id=2, Name="Áo thun", ProductCount=24 },
        new() { Id=3, Name="Quần", ProductCount=16 },
        new() { Id=4, Name="Váy đầm", ProductCount=21 },
        new() { Id=5, Name="Áo khoác", ProductCount=9 }
    ];

    public static List<PersonViewModel> Employees { get; } =
    [
        new() { Id=1, Name="Trần Minh", Email="minh@fashion.vn", Phone="0912 345 678", Role="Quản trị viên", Status="Hoạt động" },
        new() { Id=2, Name="Nguyễn Lan", Email="lan@fashion.vn", Phone="0988 111 222", Role="Nhân viên bán hàng", Status="Hoạt động" },
        new() { Id=3, Name="Lê Tuấn", Email="tuan@fashion.vn", Phone="0977 333 444", Role="Kế toán", Status="Nghỉ phép" },
        new() { Id=4, Name="Phạm Hoa", Email="hoa@fashion.vn", Phone="0966 555 777", Role="Quản kho", Status="Hoạt động" }
    ];

    public static List<PersonViewModel> Users { get; } =
    [
        new() { Id=1, Name="Nguyễn Minh Anh", Email="anh@gmail.com", Phone="0912 456 789", Role="Khách hàng", Status="Hoạt động" },
        new() { Id=2, Name="Trần Quốc Bảo", Email="bao@gmail.com", Phone="0988 123 456", Role="Khách hàng", Status="Hoạt động" },
        new() { Id=3, Name="Lê Thu Hà", Email="ha@gmail.com", Phone="0977 456 123", Role="Khách hàng", Status="Tạm khóa" },
        new() { Id=4, Name="Trần Minh", Email="minh@fashion.vn", Phone="0903 222 111", Role="Quản trị viên", Status="Hoạt động" }
    ];

    public static List<InventoryViewModel> Inventory { get; } =
    [
        new() { Id=1, Name="Áo sơ mi linen", Image=Products[0].Image, Sku="ASM-WHT-M", Stock=20, Reserved=2, Location="K1-A3" },
        new() { Id=2, Name="Váy hoa midi", Image=Products[1].Image, Sku="VD-HOA-S", Stock=14, Reserved=3, Location="K2-B1" },
        new() { Id=3, Name="Quần jean slim", Image=Products[2].Image, Sku="QJ-BLU-M", Stock=3, Reserved=1, Location="K1-C4" },
        new() { Id=4, Name="Áo khoác denim", Image=Products[3].Image, Sku="AK-DEN-L", Stock=0, Reserved=0, Location="K3-A1" }
    ];

    public static List<PromotionViewModel> Promotions { get; } =
    [
        new() { Id=1, Name="Ưu đãi tháng mới", Code="NEWYEAR20", Discount="20%", Period="01/11 - 30/11", Status="Đang chạy" },
        new() { Id=2, Name="Combo mùa đông", Code="WINTER150", Discount="Giảm 150K", Period="15/11 - 15/12", Status="Đang chạy" },
        new() { Id=3, Name="Giao hàng ưu đãi", Code="SHIP0D", Discount="Miễn phí ship", Period="20/11 - 25/11", Status="Sắp diễn ra" }
    ];

    public static List<OrderViewModel> Orders { get; } =
    [
        new() { Code="DH-26011", Customer="Nguyễn Minh Anh", Date="19/11/2026", Total=1450000, Status="Hoàn tất" },
        new() { Code="DH-26010", Customer="Trần Quốc Bảo", Date="19/11/2026", Total=890000, Status="Đang giao" },
        new() { Code="DH-26009", Customer="Lê Thu Hà", Date="18/11/2026", Total=2240000, Status="Chờ xử lý" },
        new() { Code="DH-26008", Customer="Phạm Văn Nam", Date="18/11/2026", Total=720000, Status="Đang giao" }
    ];

    public static Dictionary<string, List<ProductViewModel>> ViewedHistory { get; } = new(StringComparer.OrdinalIgnoreCase);
    public static Dictionary<string, List<ProductViewModel>> PurchaseHistory { get; } = new(StringComparer.OrdinalIgnoreCase);
    public static List<CustomerOrderViewModel> CustomerOrders { get; } = [];

    public static List<ReviewViewModel> Reviews { get; } =
    [
        new() { Id=1, Customer="Nguyễn Minh Anh", Product="Áo sơ mi linen", Rating=5, Comment="Chất vải đẹp, form dáng vừa người.", Date="19/11/2026", Status="Đã duyệt" },
        new() { Id=2, Customer="Trần Quốc Bảo", Product="Quần jean slim", Rating=4, Comment="Quần vừa và đẹp, giao hàng nhanh.", Date="18/11/2026", Status="Chờ duyệt" },
        new() { Id=3, Customer="Lê Thu Hà", Product="Váy hoa midi", Rating=5, Comment="Rất đẹp và giống hình.", Date="18/11/2026", Status="Đã duyệt" }
    ];
}
