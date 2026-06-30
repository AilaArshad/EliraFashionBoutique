using EliraFashionBoutique.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EliraFashionBoutique.Controllers;

public class OrdersController : Controller
{
    private readonly EliraDbContext _context;

    public OrdersController(EliraDbContext context)
    {
        _context = context;
    }

    // GET: /Orders
    public async Task<IActionResult> Index()
    {
        ViewBag.Customers = await _context.Customers.ToListAsync();
        return View();
    }

    // GET: /api/customer-orders
    [HttpGet("api/customer-orders")]
    public async Task<IActionResult> GetAllOrders()
    {
        var orders = await _context.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.Payment)
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new
            {
                orderId = o.OrderId,
                customerId = o.CustomerId,
                customerName = o.CustomerName,
                guestEmail = o.GuestEmail,
                guestPhoneNo = o.GuestPhoneNo,
                orderDate = o.OrderDate,
                status = o.Status,
                totalAmount = o.TotalAmount,
                discountedAmount = o.DiscountedAmount,
                finalAmount = o.FinalAmount,
                shippingAddress = o.ShippingAddress,
                items = o.OrderItems.Select(i => new
                {
                    orderItemId = i.OrderItemId,
                    variantId = i.VariantId,
                    quantity = i.Quantity,
                    unitPrice = i.UnitPrice,
                    discountedAmount = i.DiscountedAmount,
                    subtotal = i.Subtotal,
                    productName = i.Variant != null && i.Variant.Product != null ? i.Variant.Product.ProductName : "Unknown Product",
                    variantSize = i.Variant != null && i.Variant.Size != null ? i.Variant.Size.SizeName : "N/A",
                    variantColor = i.Variant != null && i.Variant.Color != null ? i.Variant.Color.ColorName : "N/A",
                    sku = i.Variant != null ? i.Variant.VariantSKU : ""
                }),
                payment = o.Payment == null ? null : new
                {
                    paymentId = o.Payment.PaymentId,
                    amount = o.Payment.Amount,
                    paymentStatus = o.Payment.PaymentStatus,
                    paidAt = o.Payment.PaidAt
                }
            })
            .ToListAsync();

        return Json(orders);
    }

    // GET: /api/customer-orders/{orderId}
    [HttpGet("api/customer-orders/{orderId}")]
    public async Task<IActionResult> GetOrderDetails(int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.Payment)
            .Where(o => o.OrderId == orderId)
            .Select(o => new
            {
                orderId = o.OrderId,
                customerId = o.CustomerId,
                customerName = o.CustomerName,
                guestEmail = o.GuestEmail,
                guestPhoneNo = o.GuestPhoneNo,
                orderDate = o.OrderDate,
                status = o.Status,
                totalAmount = o.TotalAmount,
                discountedAmount = o.DiscountedAmount,
                finalAmount = o.FinalAmount,
                shippingAddress = o.ShippingAddress,
                items = o.OrderItems.Select(i => new
                {
                    orderItemId = i.OrderItemId,
                    variantId = i.VariantId,
                    quantity = i.Quantity,
                    unitPrice = i.UnitPrice,
                    discountedAmount = i.DiscountedAmount,
                    subtotal = i.Subtotal,
                    productName = i.Variant != null && i.Variant.Product != null ? i.Variant.Product.ProductName : "Unknown Product",
                    variantSize = i.Variant != null && i.Variant.Size != null ? i.Variant.Size.SizeName : "N/A",
                    variantColor = i.Variant != null && i.Variant.Color != null ? i.Variant.Color.ColorName : "N/A",
                    sku = i.Variant != null ? i.Variant.VariantSKU : ""
                }),
                payment = o.Payment == null ? null : new
                {
                    paymentId = o.Payment.PaymentId,
                    amount = o.Payment.Amount,
                    paymentStatus = o.Payment.PaymentStatus,
                    paidAt = o.Payment.PaidAt
                }
            })
            .FirstOrDefaultAsync();

        if (order == null)
        {
            return NotFound(new { detail = "Order not found." });
        }

        return Json(order);
    }

    // POST: /api/customer-orders
    // Accepts the Order model directly from the frontend (no DTOs).
    // Creates Order + Order_Item rows inside a transaction, then initializes a Payment record as "Pending".
    [HttpPost("api/customer-orders")]
    public async Task<IActionResult> CreateOrder([FromBody] Order input)
    {
        if (input == null || input.OrderItems == null || !input.OrderItems.Any())
        {
            return BadRequest(new { detail = "Order must contain at least one item." });
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Build the Order entity
            var order = new Order
            {
                CustomerId = input.CustomerId,
                CustomerName = input.CustomerName,
                GuestEmail = input.GuestEmail,
                GuestPhoneNo = input.GuestPhoneNo,
                ShippingAddress = input.ShippingAddress,
                Status = "Pending",
                OrderDate = DateTime.Now,
                TotalAmount = 0,
                DiscountedAmount = 0,
                FinalAmount = 0
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(); // generates OrderId

            decimal totalAmount = 0m;
            decimal totalItemDiscounts = 0m;

            // Create each Order_Item row
            foreach (var itemInput in input.OrderItems)
            {
                // Validate the variant exists
                var variant = await _context.ProductVariants.FindAsync(itemInput.VariantId);
                if (variant == null)
                {
                    throw new Exception($"Product Variant with ID {itemInput.VariantId} not found.");
                }

                // Use the price from the UI payload; fall back to variant price if zero
                decimal unitPrice = itemInput.UnitPrice != 0
                    ? itemInput.UnitPrice
                    : (variant.VariantPrice ?? 0);

                decimal subtotal = (unitPrice * itemInput.Quantity) - itemInput.DiscountedAmount;

                var orderItem = new OrderItem
                {
                    OrderId = order.OrderId,
                    VariantId = itemInput.VariantId,
                    Quantity = itemInput.Quantity,
                    UnitPrice = unitPrice,
                    DiscountedAmount = itemInput.DiscountedAmount,
                    Subtotal = subtotal
                };

                _context.OrderItems.Add(orderItem);

                totalAmount += (unitPrice * itemInput.Quantity);
                totalItemDiscounts += itemInput.DiscountedAmount;
            }

            // Aggregate totals on the parent Order
            order.TotalAmount = totalAmount;
            order.DiscountedAmount = input.DiscountedAmount > 0
                ? input.DiscountedAmount
                : totalItemDiscounts;
            order.FinalAmount = totalAmount - order.DiscountedAmount;

            // Initialize Payment record with "Pending" status (CoD)
            var payment = new Payment
            {
                OrderId = order.OrderId,
                Amount = order.FinalAmount,
                PaymentStatus = "Pending",
                PaidAt = DateTime.Now
            };

            _context.Payments.Add(payment);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            System.Console.WriteLine($"[ORDER LOG] Created Order #{order.OrderId} for \"{order.CustomerName}\" | Items: {input.OrderItems.Count} | Final: {order.FinalAmount:C}");

            return StatusCode(201, new { message = "Order placed successfully!", orderId = order.OrderId });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { detail = ex.Message });
        }
    }

    // POST: /api/customer-orders/update-status
    // Admin action: updates order Status. If status transitions to "Shipped",
    // automatically marks the associated Payment as "Paid" with the current server time (CoD automation).
    [HttpPost("api/customer-orders/update-status")]
    public async Task<IActionResult> UpdateOrderStatus([FromBody] Order input)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.Status))
        {
            return BadRequest(new { detail = "Order ID and Status are required." });
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var order = await _context.Orders
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.OrderId == input.OrderId);

            if (order == null)
            {
                return NotFound(new { detail = "Order not found." });
            }

            string previousStatus = order.Status;
            if (!string.IsNullOrWhiteSpace(input.Status))
            {
                order.Status = input.Status;
            }
            if (!string.IsNullOrWhiteSpace(input.ShippingAddress))
            {
                order.ShippingAddress = input.ShippingAddress;
            }

            System.Console.WriteLine($"[ORDER LOG] Update for Order #{order.OrderId}: Status=\"{order.Status}\", Address=\"{order.ShippingAddress}\"");

            // ─── CoD Automation: Shipped → Payment = Paid ───
            if (previousStatus != "Shipped" && input.Status == "Shipped" && order.Payment != null)
            {
                order.Payment.PaymentStatus = "Paid";
                order.Payment.PaidAt = DateTime.Now;

                System.Console.WriteLine($"[PAYMENT LOG] Payment #{order.Payment.PaymentId} for Order #{order.OrderId} automatically set to PAID at {order.Payment.PaidAt}");
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { message = "Order status updated successfully!" });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { detail = ex.Message });
        }
    }

    // DELETE: /api/customer-orders/{orderId}
    [HttpDelete("api/customer-orders/{orderId}")]
    public async Task<IActionResult> DeleteOrder(int orderId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return NotFound(new { detail = "Order not found." });
            }

            if (order.Payment != null)
            {
                _context.Payments.Remove(order.Payment);
            }

            if (order.OrderItems != null && order.OrderItems.Any())
            {
                _context.OrderItems.RemoveRange(order.OrderItems);
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            System.Console.WriteLine($"[ORDER LOG] Deleted Order #{orderId}");

            return Ok(new { message = "Order deleted successfully!" });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { detail = ex.Message });
        }
    }

    // GET: /api/customers
    [HttpGet("api/customers")]
    public async Task<IActionResult> GetCustomers()
    {
        var customers = await _context.Customers
            .Select(c => new
            {
                customerId = c.CustomerId,
                fullName = c.FullName,
                phoneNo = c.PhoneNo,
                address = c.Address,
                city = c.City,
                postalCode = c.PostalCode,
                country = c.Country
            })
            .ToListAsync();

        return Json(customers);
    }
}
