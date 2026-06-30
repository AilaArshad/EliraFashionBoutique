namespace EliraFashionBotiqueWebAPI.Models.DTOs
{
    public class CustomerHistoryFilter
    {
        public int? CustomerId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}