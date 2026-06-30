namespace EliraFashionBotiqueWebAPI.Models.DTOs
{
    public class ProductPerformanceDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SubcategoryName { get; set; } = string.Empty;
        public int TotalUnitsSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public int NumberOfOrders { get; set; }
    }
}