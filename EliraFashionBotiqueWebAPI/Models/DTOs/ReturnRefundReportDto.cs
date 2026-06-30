namespace EliraFashionBotiqueWebAPI.Models.DTOs
{
    public class ReturnRefundReportDto
    {
        public int ReturnId { get; set; }
        public int OrderId { get; set; }
        public DateTime ReturnDate { get; set; }
        public string ReturnStatus { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string VariantSKU { get; set; } = string.Empty;
        public int QuantityReturned { get; set; }
        public string? ReturnedCondition { get; set; }
        public string? ResolutionType { get; set; }
        public decimal? RefundAmount { get; set; }
        public string? RefundMethod { get; set; }
        public string? RefundStatus { get; set; }
    }
}