namespace EliraFashionBotiqueWebAPI.Models.DTOs
{
    public class ReturnRefundFilter
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? ReturnStatus { get; set; }
    }
}