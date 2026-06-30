using EliraFashionBoutique.Models;
using EliraFashionBoutique.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace EliraFashionBoutique.Controllers
{
    public class HomeController : Controller
    {
        private readonly EliraDbContext _context;
        private static readonly string ThresholdsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "reorder_thresholds.json");

        public HomeController(EliraDbContext context)
        {
            _context = context;
        }

        private Dictionary<string, int> GetThresholds()
        {
            if (!System.IO.File.Exists(ThresholdsFilePath))
            {
                return new Dictionary<string, int>
                {
                    { "LNN-SMR-SH-XL", 15 },
                    { "CTN-KRT-M", 12 },
                    { "BDL-LHG-RED", 5 }
                };
            }

            try
            {
                var text = System.IO.File.ReadAllText(ThresholdsFilePath);
                return JsonSerializer.Deserialize<Dictionary<string, int>>(text) ?? new Dictionary<string, int>();
            }
            catch
            {
                return new Dictionary<string, int>();
            }
        }

        public async Task<IActionResult> Index()
        {
            // 1. Gross Revenue (Completed, Delivered or Shipped status)
            var grossRevenue = await _context.Orders
                .Where(o => o.Status == "Completed" || o.Status == "Delivered" || o.Status == "Shipped")
                .SumAsync(o => (decimal?)o.FinalAmount) ?? 0m;

            // 2. Order Volume
            var orderVolume = await _context.Orders.CountAsync();

            // 3. Registered Accounts
            var registeredAccounts = await _context.Users.CountAsync();
            var verifiedAccounts = await _context.Users.CountAsync(u => u.IsEmailVerified == true);
            var emailVerificationPercentage = registeredAccounts > 0
                ? Math.Round(((double)verifiedAccounts / registeredAccounts) * 100, 0)
                : 0.0;

            // 4. Reorder Alerts (Evaluated via SKU mapping in thresholds file)
            var inventories = await _context.Inventories
                .Include(i => i.Variant)
                .ToListAsync();

            var thresholds = GetThresholds();
            int reorderAlerts = 0;

            foreach (var inv in inventories)
            {
                if (inv.Variant != null)
                {
                    string sku = inv.Variant.VariantSKU ?? ("Variant-" + inv.VariantId);
                    int threshold = thresholds.TryGetValue(sku, out var val) ? val : 10;
                    if (inv.QuantityAvailable <= threshold)
                    {
                        reorderAlerts++;
                    }
                }
            }

            // 5. Recent Customer Sales Ledger (Projection avoids null references and performs clean SQL joins)
            var recentOrders = await _context.Orders
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .Select(o => new RecentOrderDto
                {
                    OrderId = o.OrderId,
                    CustomerName = !string.IsNullOrEmpty(o.CustomerName)
                        ? o.CustomerName
                        : (o.Customer != null ? o.Customer.FullName : "Guest Customer"),
                    Email = !string.IsNullOrEmpty(o.GuestEmail)
                        ? o.GuestEmail
                        : (o.Customer != null && o.Customer.User != null ? o.Customer.User.Email : "No Email"),
                    IsGuest = o.CustomerId == null,
                    OrderDate = o.OrderDate,
                    FinalAmount = o.FinalAmount,
                    Status = o.Status
                })
                .ToListAsync();

            // 6. Active Category Campaigns
            var activePromos = await _context.Promotions
                .Include(p => p.SubCategory)
                .Where(p => p.IsActive == true && (p.StartDate == null || p.StartDate <= DateTime.Now) && (p.EndDate == null || p.EndDate >= DateTime.Now))
                .ToListAsync();

            var activeCampaigns = new List<ActiveCampaignDto>();
            foreach (var p in activePromos)
            {
                var progress = 100;
                if (p.StartDate.HasValue && p.EndDate.HasValue && p.EndDate > p.StartDate)
                {
                    var total = (p.EndDate.Value - p.StartDate.Value).Ticks;
                    var elapsed = (DateTime.Now - p.StartDate.Value).Ticks;
                    progress = (int)Math.Clamp((double)elapsed / total * 100, 0, 100);
                }
                activeCampaigns.Add(new ActiveCampaignDto
                {
                    DiscountName = p.DiscountName ?? "Special Campaign",
                    PromotionDiscount = p.PromotionDiscount ?? "Discount",
                    AppliedTo = p.SubCategory != null ? p.SubCategory.SubcategoryName : "All Subcategories",
                    ProgressPercentage = progress
                });
            }

            var viewModel = new DashboardViewModel
            {
                GrossRevenue = grossRevenue,
                OrderVolume = orderVolume,
                RegisteredAccounts = registeredAccounts,
                EmailVerificationPercentage = emailVerificationPercentage,
                ReorderAlerts = reorderAlerts,
                RecentOrders = recentOrders,
                ActiveCampaigns = activeCampaigns
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
