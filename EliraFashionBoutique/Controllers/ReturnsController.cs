using EliraFashionBoutique.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;

namespace EliraFashionBoutique.Controllers;

[Authorize]
public class ReturnsController : Controller
{
    private readonly EliraDbContext _context;

    public ReturnsController(EliraDbContext context)
    {
        _context = context;
    }

    // GET: /Returns
    public IActionResult Index()
    {
        return View();
    }

    // GET: /api/Returns
    [HttpGet("api/Returns")]
    public async Task<IActionResult> GetReturns()
    {
        try
        {
            var returnOrders = await _context.ReturnOrders
                .Include(r => r.Order)
                .Include(r => r.ReturnItems)
                    .ThenInclude(ri => ri.OrderItem)
                        .ThenInclude(oi => oi.Variant!)
                            .ThenInclude(v => v.Product)
                .OrderByDescending(r => r.ReturnDate)
                .ToListAsync();

            var refunds = await _context.Refunds.ToListAsync();

            var list = returnOrders.Select(r => {
                var firstItem = r.ReturnItems.FirstOrDefault();
                var refund = refunds.FirstOrDefault(refd => refd.ReturnId == r.ReturnId);

                decimal refundAmt = refund?.RefundAmount ?? 0m;

                return new {
                    returnId = r.ReturnId,
                    claimId = $"RET-2026-{r.ReturnId:D4}",
                    orderId = r.OrderId,
                    orderRef = $"ORD-2026-{r.OrderId}",
                    customerName = r.Order?.CustomerName ?? "Guest",
                    returnDate = r.ReturnDate.ToString("yyyy-MM-dd"),
                    resolutionType = firstItem?.ResolutionType ?? "Full Refund to Bank",
                    customReasonText = r.CustomReasonText ?? "No reason specified",
                    refundAmount = refundAmt,
                    refundAmountFormatted = $"Rs. {refundAmt:N0}",
                    returnStatus = r.ReturnStatus,
                    paymentStatus = refund?.RefundStatus ?? "Escrow Hold",
                    bankName = refund?.BankName ?? "-",
                    bankAccount = refund?.BankAccountNumber ?? "-"
                };
            }).ToList();

            return Json(list);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { detail = ex.Message });
        }
    }

    // GET: /api/Returns/eligible-orders
    [HttpGet("api/Returns/eligible-orders")]
    public async Task<IActionResult> GetEligibleOrders()
    {
        try
        {
            var orders = await _context.Orders
                .Where(o => o.Status == "Shipped")
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new {
                    orderId = o.OrderId,
                    orderRef = $"ORD-2026-{o.OrderId}",
                    customerName = o.CustomerName
                })
                .ToListAsync();

            return Json(orders);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { detail = ex.Message });
        }
    }

    // GET: /api/Returns/orders/{orderId}/items
    [HttpGet("api/Returns/orders/{orderId}/items")]
    public async Task<IActionResult> GetOrderItems(int orderId)
    {
        try
        {
            // Fetch OrderItemIds that are already returned/refunded (with status 'Pending Audit' or 'Approved')
            var activeReturnedOrderItemIds = await _context.ReturnItems
                .Include(ri => ri.ReturnOrder)
                .Where(ri => ri.ReturnOrder.OrderId == orderId && 
                            (ri.ReturnOrder.ReturnStatus == "Pending Audit" || ri.ReturnOrder.ReturnStatus == "Approved"))
                .Select(ri => ri.OrderItemId)
                .ToListAsync();

            var orderItems = await _context.OrderItems
                .Include(oi => oi.Variant!)
                    .ThenInclude(v => v.Product)
                .Where(oi => oi.OrderId == orderId && !activeReturnedOrderItemIds.Contains(oi.OrderItemId))
                .Select(oi => new {
                    orderItemId = oi.OrderItemId,
                    variantId = oi.VariantId,
                    productName = oi.Variant != null && oi.Variant.Product != null ? oi.Variant.Product.ProductName : "Unknown Product",
                    sku = oi.Variant != null ? oi.Variant.VariantSKU : "N/A",
                    price = oi.UnitPrice,
                    quantity = oi.Quantity
                })
                .ToListAsync();

            return Json(orderItems);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { detail = ex.Message });
        }
    }

    // POST: /api/Returns
    [HttpPost("api/Returns")]
    public async Task<IActionResult> CreateReturn([FromBody] ReturnInputDto input)
    {
        if (input == null || input.OrderId <= 0 || input.OrderItemId <= 0 || input.QuantityReturned <= 0 || string.IsNullOrWhiteSpace(input.CustomReasonText))
        {
            return BadRequest(new { detail = "Invalid return input data." });
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Validate order and item exist
            var order = await _context.Orders
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.OrderId == input.OrderId);

            if (order == null)
            {
                return NotFound(new { detail = "Order not found." });
            }

            var orderItem = await _context.OrderItems.FindAsync(input.OrderItemId);
            if (orderItem == null || orderItem.OrderId != input.OrderId)
            {
                return BadRequest(new { detail = "Order item does not belong to the selected order." });
            }

            // Validate duplicate return
            var existingReturn = await _context.ReturnItems
                .Include(ri => ri.ReturnOrder)
                .AnyAsync(ri => ri.OrderItemId == input.OrderItemId &&
                                (ri.ReturnOrder.ReturnStatus == "Pending Audit" || ri.ReturnOrder.ReturnStatus == "Approved"));

            if (existingReturn)
            {
                return BadRequest(new { detail = "This item has already been returned or has an active pending return claim." });
            }

            // Insert into Return_Order
            var returnOrder = new ReturnOrder
            {
                OrderId = input.OrderId,
                ReturnDate = DateTime.Now,
                ReturnStatus = "Pending Audit",
                CustomReasonText = input.CustomReasonText,
                ProcessedBy = null
            };

            _context.ReturnOrders.Add(returnOrder);
            await _context.SaveChangesAsync(); // Generates ReturnId

            // Insert into Return_Item
            var returnItem = new ReturnItem
            {
                ReturnId = returnOrder.ReturnId,
                OrderItemId = input.OrderItemId,
                QuantityReturned = input.QuantityReturned,
                ReturnedCondition = input.ReturnedCondition ?? "Damaged",
                ResolutionType = "Full Refund to Bank"
            };

            _context.ReturnItems.Add(returnItem);

            // Insert into Refund
            var refund = new Refund
            {
                ReturnId = returnOrder.ReturnId,
                PaymentId = order.Payment?.PaymentId,
                RefundAmount = orderItem.UnitPrice * input.QuantityReturned,
                RefundMethod = "Bank Transfer",
                RefundStatus = "Pending Audit", // Or Escrow Hold
                RefundDate = null,
                BankName = input.BankName,
                BankAccountNumber = input.BankAccountNumber
            };

            _context.Refunds.Add(refund);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return StatusCode(201, new { 
                message = "Record verified and added safely.", 
                returnId = returnOrder.ReturnId,
                claimId = $"RET-2026-{returnOrder.ReturnId:D4}"
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { detail = ex.Message });
        }
    }

    // PATCH: /api/Returns/{returnId}/status
    [HttpPatch("api/Returns/{returnId}/status")]
    public async Task<IActionResult> UpdateStatus(int returnId, [FromBody] StatusInputDto input)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.Status))
        {
            return BadRequest(new { detail = "Status is required." });
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var returnOrder = await _context.ReturnOrders
                .Include(r => r.ReturnItems)
                .FirstOrDefaultAsync(r => r.ReturnId == returnId);

            if (returnOrder == null)
            {
                return NotFound(new { detail = "Return record not found." });
            }

            var refund = await _context.Refunds.FirstOrDefaultAsync(refd => refd.ReturnId == returnId);

            string previousStatus = returnOrder.ReturnStatus;
            returnOrder.ReturnStatus = input.Status;

            if (refund != null)
            {
                if (input.Status == "Approved")
                {
                    refund.RefundStatus = "Processing Settlement";

                    // Automatic Inventory Sync: increment quantity available for returned items
                    foreach (var ri in returnOrder.ReturnItems)
                    {
                        var orderItem = await _context.OrderItems.FindAsync(ri.OrderItemId);
                        if (orderItem != null)
                        {
                            var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.VariantId == orderItem.VariantId);
                            if (inventory != null)
                            {
                                inventory.QuantityAvailable += ri.QuantityReturned;
                            }
                            else
                            {
                                // If inventory record does not exist for some reason, create it
                                var newInventory = new Inventory
                                {
                                    VariantId = orderItem.VariantId,
                                    QuantityAvailable = ri.QuantityReturned
                                };
                                _context.Inventories.Add(newInventory);
                            }
                        }
                    }
                }
                else if (input.Status == "Rejected")
                {
                    refund.RefundStatus = "Voided / Cancelled";
                }
                else
                {
                    refund.RefundStatus = "Escrow Hold";
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { message = "Status updated successfully!" });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { detail = ex.Message });
        }
    }
}

public class ReturnInputDto
{
    public int OrderId { get; set; }
    public int OrderItemId { get; set; }
    public int QuantityReturned { get; set; }
    public string CustomReasonText { get; set; } = string.Empty;
    public string? ReturnedCondition { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
}

public class StatusInputDto
{
    public string Status { get; set; } = string.Empty;
}
