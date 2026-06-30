using System;
using System.Collections.Generic;

namespace EliraFashionBoutique.ViewModels
{
    public class DashboardViewModel
    {
        public decimal GrossRevenue { get; set; }
        public int OrderVolume { get; set; }
        public int RegisteredAccounts { get; set; }
        public double EmailVerificationPercentage { get; set; }
        public int ReorderAlerts { get; set; }
        public List<RecentOrderDto> RecentOrders { get; set; } = new();
        public List<ActiveCampaignDto> ActiveCampaigns { get; set; } = new();
    }

    public class RecentOrderDto
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsGuest { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal FinalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class ActiveCampaignDto
    {
        public string DiscountName { get; set; } = string.Empty;
        public string PromotionDiscount { get; set; } = string.Empty;
        public string AppliedTo { get; set; } = string.Empty;
        public int ProgressPercentage { get; set; }
    }
}
