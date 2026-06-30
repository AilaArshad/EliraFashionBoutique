namespace EliraFashionBotiqueWebAPI.Models.DTOs
{
    public class CustomerOrderHistoryDto
    {
        public int CustomerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNo { get; set; }
        public int TotalOrders { get; set; }
        public decimal? TotalSpent { get; set; }
        public DateTime? LastOrderDate { get; set; }
    }
}