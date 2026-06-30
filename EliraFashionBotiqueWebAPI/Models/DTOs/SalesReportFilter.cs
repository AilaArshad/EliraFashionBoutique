namespace EliraFashionBotiqueWebAPI.Models.DTOs
{
    public class SalesReportFilter
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? CategoryId { get; set; }
        public string? Status { get; set; }
    }
}