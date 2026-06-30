namespace EliraFashionBotiqueWebAPI.Models.DTOs
{
    public class LowStockReportDto
    {
        public string ProductName { get; set; } = string.Empty;
        public string VariantSKU { get; set; } = string.Empty;
        public string? SizeName { get; set; }
        public string? ColorName { get; set; }
        public int QuantityAvailable { get; set; }
        public int ReorderLevel { get; set; }
        public int UnitsNeeded { get; set; }
        public string SubcategoryName { get; set; } = string.Empty;
    }
}