using EliraFashionBoutique.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EliraFashionBoutique.Controllers;

public class PurchaseOrdersController : Controller
{
    private readonly EliraDbContext _context;

    public PurchaseOrdersController(EliraDbContext context)
    {
        _context = context;
    }

    // GET: /PurchaseOrders
    public IActionResult Index()
    {
        return View();
    }

    // GET: /api/suppliers
    [HttpGet("api/suppliers")]
    public async Task<IActionResult> GetSuppliers()
    {
        var suppliers = await _context.Suppliers
            .Select(s => new {
                id = s.SupplierId,
                name = s.SupplierName,
                category = s.Category
            })
            .ToListAsync();

        return Json(suppliers);
    }

    // GET: /api/suppliers/{supplierId}/orders
    [HttpGet("api/suppliers/{supplierId}/orders")]
    public async Task<IActionResult> GetSupplierOrders(int supplierId)
    {
        var orders = await _context.PurchaseOrders
            .Where(o => o.SupplierId == supplierId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new {
                purchaseOrderId = o.PurchaseOrderId,
                expectedDeliveryDate = o.ExpectedDeliveryDate.ToString("yyyy-MM-dd"),
                status = o.Status,
                totalAmount = o.TotalAmount,
                items = o.PurchaseOrderItems.Select(i => new {
                    variantId = i.VariantId,
                    quantityOrdered = i.QuantityOrdered,
                    subtotal = i.Subtotal,
                    variantDetails = new {
                        sku = i.Variant != null ? i.Variant.VariantSKU : ("SKU-" + i.VariantId)
                    }
                })
            })
            .ToListAsync();

        return Json(orders);
    }

    // GET: /api/orders/{orderId}
    [HttpGet("api/orders/{orderId}")]
    public async Task<IActionResult> GetOrderDetails(int orderId)
    {
        var order = await _context.PurchaseOrders
            .Where(o => o.PurchaseOrderId == orderId)
            .Select(o => new {
                purchaseOrderId = o.PurchaseOrderId,
                expectedDeliveryDate = o.ExpectedDeliveryDate.ToString("yyyy-MM-dd"),
                status = o.Status,
                totalAmount = o.TotalAmount,
                items = o.PurchaseOrderItems.Select(i => new {
                    variantId = i.VariantId,
                    quantityOrdered = i.QuantityOrdered,
                    subtotal = i.Subtotal
                })
            })
            .FirstOrDefaultAsync();

        if (order == null)
        {
            return NotFound(new { detail = "Order not found." });
        }

        return Json(order);
    }

    // POST: /api/orders
    [HttpPost("api/orders")]
    public async Task<IActionResult> CreateOrder([FromBody] OrderInputDto input)
    {
        if (input == null || input.Items == null || !input.Items.Any())
        {
            return BadRequest(new { detail = "Manifest cannot be empty." });
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Verify Supplier exists
            var supplier = await _context.Suppliers.FindAsync(input.SupplierId);
            if (supplier == null)
            {
                return BadRequest(new { detail = "Supplier not found." });
            }

            // Create Parent Order
            var order = new PurchaseOrder
            {
                SupplierId = input.SupplierId,
                ExpectedDeliveryDate = input.ExpectedDeliveryDate,
                Status = input.Status,
                TotalAmount = 0,
                CreatedAt = DateTime.Now
            };

            _context.PurchaseOrders.Add(order);
            await _context.SaveChangesAsync();

            decimal calculatedTotal = 0;

            // Add Child Items
            foreach (var itemInput in input.Items)
            {
                var variant = await _context.ProductVariants.FindAsync(itemInput.VariantId);
                if (variant == null)
                {
                    throw new Exception($"Product Variant with ID {itemInput.VariantId} not found.");
                }

                decimal price = variant.VariantPrice ?? 0;
                decimal itemSubtotal = price * itemInput.QuantityOrdered;
                calculatedTotal += itemSubtotal;

                var orderItem = new PurchaseOrderItem
                {
                    PurchaseOrderId = order.PurchaseOrderId,
                    VariantId = itemInput.VariantId,
                    QuantityOrdered = itemInput.QuantityOrdered,
                    Subtotal = itemSubtotal
                };

                _context.PurchaseOrderItems.Add(orderItem);

                // INVENTORY PROVISION: Check status on creation
                if (input.Status == "Approved")
                {
                    var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.VariantId == itemInput.VariantId);
                    if (inventory == null)
                    {
                        inventory = new Inventory
                        {
                            VariantId = itemInput.VariantId,
                            QuantityAvailable = itemInput.QuantityOrdered
                        };
                        _context.Inventories.Add(inventory);
                    }
                    else
                    {
                        inventory.QuantityAvailable += itemInput.QuantityOrdered;
                    }
                }
            }

            order.TotalAmount = calculatedTotal;
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return StatusCode(201, new { message = "Order committed successfully!", id = order.PurchaseOrderId });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { detail = ex.Message });
        }
    }

    // POST: /api/purchaseorders/update
    [HttpPost("api/purchaseorders/update")]
    public async Task<IActionResult> UpdateOrder([FromBody] OrderUpdateInputDto input)
    {
        if (input == null || input.ManifestItems == null || !input.ManifestItems.Any())
        {
            return BadRequest(new { detail = "Items manifest list is required." });
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Verify order exists
            var order = await _context.PurchaseOrders
                .Include(o => o.PurchaseOrderItems)
                .FirstOrDefaultAsync(o => o.PurchaseOrderId == input.PurchaseOrderId);

            if (order == null)
            {
                return NotFound(new { detail = "Purchase Order not found." });
            }

            string previousStatus = order.Status;

            // Separate items in DB by VariantId
            var existingItemsMap = order.PurchaseOrderItems.ToDictionary(item => item.VariantId);

            // New manifest items from client
            var incomingItems = input.ManifestItems;
            var incomingVariantIds = incomingItems.Select(i => i.VariantId).ToHashSet();

            // 1. DELETE: Completely remove any rows from database that the user deleted on the UI
            var itemsToDelete = order.PurchaseOrderItems.Where(item => !incomingVariantIds.Contains(item.VariantId)).ToList();
            _context.PurchaseOrderItems.RemoveRange(itemsToDelete);

            decimal calculatedTotal = 0;

            // 2. ADD & UPDATE
            foreach (var itemInput in incomingItems)
            {
                var variant = await _context.ProductVariants.FindAsync(itemInput.VariantId);
                if (variant == null)
                {
                    throw new Exception($"Product Variant with ID {itemInput.VariantId} not found.");
                }

                decimal price = itemInput.UnitCost;
                decimal itemSubtotal = price * itemInput.Qty;
                calculatedTotal += itemSubtotal;

                if (existingItemsMap.TryGetValue(itemInput.VariantId, out var existingItem))
                {
                    // UPDATE: Save any changes made to existing rows
                    existingItem.QuantityOrdered = itemInput.Qty;
                    existingItem.Subtotal = itemSubtotal;
                }
                else
                {
                    // ADD: Insert any newly added variant rows into the database
                    var newItem = new PurchaseOrderItem
                    {
                        PurchaseOrderId = input.PurchaseOrderId,
                        VariantId = itemInput.VariantId,
                        QuantityOrdered = itemInput.Qty,
                        Subtotal = itemSubtotal
                    };
                    _context.PurchaseOrderItems.Add(newItem);
                }

                // 3. INVENTORY ADJUSTMENT:
                // If status transitions to "Approved", ensure QuantityAvailable is accurately incremented
                if (input.Status == "Approved" && previousStatus != "Approved")
                {
                    var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.VariantId == itemInput.VariantId);
                    if (inventory == null)
                    {
                        inventory = new Inventory
                        {
                            VariantId = itemInput.VariantId,
                            QuantityAvailable = itemInput.Qty
                        };
                        _context.Inventories.Add(inventory);
                    }
                    else
                    {
                        inventory.QuantityAvailable += itemInput.Qty;
                    }
                }
            }

            // Update parent metadata
            order.SupplierId = input.SupplierId;
            order.ExpectedDeliveryDate = input.ExpectedDeliveryDate;
            order.Status = input.Status;
            order.TotalAmount = calculatedTotal;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { message = "Order updated successfully!" });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { detail = ex.Message });
        }
    }

    // GET: /api/subcategories
    [HttpGet("api/subcategories")]
    public async Task<IActionResult> GetSubCategories()
    {
        var subcategories = await _context.SubCategories
            .Select(sc => new {
                id = sc.SubCategoryId,
                name = sc.SubcategoryName
            })
            .ToListAsync();

        return Json(subcategories);
    }

    // GET: /api/products/by-subcategory
    [HttpGet("api/products/by-subcategory")]
    public async Task<IActionResult> GetProductsBySubcategory([FromQuery] int subcategory_id)
    {
        var products = await _context.Products
            .Where(p => p.SubCategoryId == subcategory_id)
            .Select(p => new {
                id = p.ProductId,
                name = p.ProductName
            })
            .ToListAsync();

        return Json(products);
    }

    // GET: /api/variants/by-product
    [HttpGet("api/variants/by-product")]
    public async Task<IActionResult> GetVariantsByProduct([FromQuery] int product_id)
    {
        var variants = await _context.ProductVariants
            .Include(v => v.Size)
            .Include(v => v.Color)
            .Where(v => v.ProductId == product_id)
            .Select(v => new {
                variantId = v.VariantId,
                sku = v.VariantSKU,
                size = v.Size != null ? v.Size.SizeName : "N/A",
                color = v.Color != null ? v.Color.ColorName : "N/A",
                price = v.VariantPrice ?? 0
            })
            .ToListAsync();

        return Json(variants);
    }

    // GET: /api/variants/{variantId}
    [HttpGet("api/variants/{variantId}")]
    public async Task<IActionResult> GetVariantDetails(int variantId)
    {
        var variant = await _context.ProductVariants
            .Where(v => v.VariantId == variantId)
            .Select(v => new {
                variantId = v.VariantId,
                productId = v.ProductId,
                sku = v.VariantSKU,
                price = v.VariantPrice ?? 0
            })
            .FirstOrDefaultAsync();

        if (variant == null)
        {
            return NotFound();
        }

        return Json(variant);
    }

    // GET: /api/products/{productId}
    [HttpGet("api/products/{productId}")]
    public async Task<IActionResult> GetProductDetails(int productId)
    {
        var product = await _context.Products
            .Where(p => p.ProductId == productId)
            .Select(p => new {
                id = p.ProductId,
                subCategoryId = p.SubCategoryId,
                name = p.ProductName
            })
            .FirstOrDefaultAsync();

        if (product == null)
        {
            return NotFound();
        }

        return Json(product);
    }
}

// Request ViewModels for Model Binding
public class OrderInputDto
{
    public int SupplierId { get; set; }
    public DateTime ExpectedDeliveryDate { get; set; }
    public string Status { get; set; } = "Pending Audit";
    public List<OrderItemInputDto> Items { get; set; } = new();
}

public class OrderItemInputDto
{
    public int VariantId { get; set; }
    public int QuantityOrdered { get; set; }
    public decimal Subtotal { get; set; }
}

public class OrderUpdateInputDto
{
    public int PurchaseOrderId { get; set; }
    public int SupplierId { get; set; }
    public DateTime ExpectedDeliveryDate { get; set; }
    public string Status { get; set; } = "Pending Audit";
    public List<OrderItemUpdateInputDto> ManifestItems { get; set; } = new();
}

public class OrderItemUpdateInputDto
{
    public int VariantId { get; set; }
    public int Qty { get; set; }
    public decimal UnitCost { get; set; }
}
