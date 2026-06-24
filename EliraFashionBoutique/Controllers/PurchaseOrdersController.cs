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
            .Where(s => s.UserId != null)
            .Select(s => new {
                id = s.SupplierId,
                name = s.SupplierName,
                category = s.Category,
                hasOrders = s.PurchaseOrders.Any()
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

            System.Console.WriteLine($"[PURCHASE ORDER LOG] Creating Purchase Order for Supplier {supplier.SupplierName} (SupplierID: {supplier.SupplierId}, Associated UserID: {supplier.UserId})");

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

            // Update the supplier's Category to match the Category Name of the purchase order items
            var firstItemInput = input.Items.FirstOrDefault();
            if (firstItemInput != null)
            {
                var firstVariant = await _context.ProductVariants
                    .Include(v => v.Product)
                        .ThenInclude(p => p.SubCategory)
                            .ThenInclude(sc => sc.Category)
                    .FirstOrDefaultAsync(v => v.VariantId == firstItemInput.VariantId);
                if (firstVariant != null && firstVariant.Product != null && firstVariant.Product.SubCategory != null && firstVariant.Product.SubCategory.Category != null)
                {
                    supplier.Category = firstVariant.Product.SubCategory.Category.CategoryName;
                }
            }

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

            if (order.Status != null && order.Status.Trim().Equals("Approved", System.StringComparison.Ordinal))
            {
                return BadRequest(new { detail = "Approved Purchase Orders are read-only and cannot be updated." });
            }

            var supplier = await _context.Suppliers.FindAsync(input.SupplierId);
            System.Console.WriteLine($"[PURCHASE ORDER LOG] Updating Purchase Order #{input.PurchaseOrderId} for Supplier {supplier?.SupplierName} (SupplierID: {input.SupplierId}, Associated UserID: {supplier?.UserId})");

            string previousStatus = order.Status;

            // Separate items in DB to properly track and sync them
            var existingItems = order.PurchaseOrderItems.ToList();
            var matchedExistingItems = new List<PurchaseOrderItem>();
            var newItemsToInsert = new List<PurchaseOrderItem>();

            decimal calculatedTotal = 0;

            // 1. ADD & UPDATE DETECTOR LOOP
            foreach (var itemInput in input.ManifestItems)
            {
                var variant = await _context.ProductVariants.FindAsync(itemInput.VariantId);
                if (variant == null)
                {
                    throw new Exception($"Product Variant with ID {itemInput.VariantId} not found.");
                }

                decimal price = itemInput.UnitCost;
                decimal itemSubtotal = price * itemInput.Qty;
                calculatedTotal += itemSubtotal;

                // Dynamically find a matching unmatched existing item
                var existingItem = existingItems
                    .FirstOrDefault(item => item.VariantId == itemInput.VariantId && !matchedExistingItems.Contains(item));

                if (existingItem != null)
                {
                    // UPDATE: Save any changes made to existing rows
                    existingItem.QuantityOrdered = itemInput.Qty;
                    existingItem.Subtotal = itemSubtotal;
                    matchedExistingItems.Add(existingItem);
                    _context.Entry(existingItem).State = EntityState.Modified;
                }
                else
                {
                    // ADD: Insert newly added variant rows dynamically
                    var newItem = new PurchaseOrderItem
                    {
                        PurchaseOrderId = order.PurchaseOrderId,
                        VariantId = itemInput.VariantId,
                        QuantityOrdered = itemInput.Qty,
                        Subtotal = itemSubtotal
                    };
                    newItemsToInsert.Add(newItem);
                }

                // 2. INVENTORY ADJUSTMENT:
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

            // 3. DELETE: Completely remove any rows from database that the user deleted on the UI
            var itemsToDelete = existingItems.Where(item => !matchedExistingItems.Contains(item)).ToList();
            foreach (var item in itemsToDelete)
            {
                order.PurchaseOrderItems.Remove(item);
                _context.PurchaseOrderItems.Remove(item);
            }

            // 4. INSERT: Add newly created item entities to parent order's tracked navigation collection
            foreach (var newItem in newItemsToInsert)
            {
                order.PurchaseOrderItems.Add(newItem);
            }

            // Update parent metadata
            order.SupplierId = input.SupplierId;
            order.ExpectedDeliveryDate = input.ExpectedDeliveryDate;
            order.Status = input.Status;
            order.TotalAmount = calculatedTotal;

            // Update the supplier's Category to match the Category Name of the purchase order items
            var firstItemInput = input.ManifestItems.FirstOrDefault();
            if (firstItemInput != null && supplier != null)
            {
                var firstVariant = await _context.ProductVariants
                    .Include(v => v.Product)
                        .ThenInclude(p => p.SubCategory)
                            .ThenInclude(sc => sc.Category)
                    .FirstOrDefaultAsync(v => v.VariantId == firstItemInput.VariantId);
                if (firstVariant != null && firstVariant.Product != null && firstVariant.Product.SubCategory != null && firstVariant.Product.SubCategory.Category != null)
                {
                    supplier.Category = firstVariant.Product.SubCategory.Category.CategoryName;
                }
            }

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

    // GET: /api/categories
    [HttpGet("api/categories")]
    public async Task<IActionResult> GetCategories()
    {
        if (!await _context.Categories.AnyAsync())
        {
            var category1 = new Category { CategoryName = "Linens & Cotton", Description = "Premium linens and cotton fabrics" };
            var category2 = new Category { CategoryName = "Bridal Wear", Description = "Luxury bridal wear collection" };
            _context.Categories.AddRange(category1, category2);
            await _context.SaveChangesAsync();

            var subcat1 = new SubCategory { CategoryId = category1.CategoryId, SubcategoryName = "Linens", SeasonType = "Summer", IsActive = true };
            var subcat2 = new SubCategory { CategoryId = category1.CategoryId, SubcategoryName = "Cotton", SeasonType = "Summer", IsActive = true };
            var subcat3 = new SubCategory { CategoryId = category2.CategoryId, SubcategoryName = "Lehengas", SeasonType = "Winter", IsActive = true };
            _context.SubCategories.AddRange(subcat1, subcat2, subcat3);
            await _context.SaveChangesAsync();

            var product1 = new Product { SubCategoryId = subcat1.SubCategoryId, ProductName = "Summer Linen Shirt", BasePrice = 1500, SKU = "LNN-SMR-SH-XL", IsActive = true };
            var product2 = new Product { SubCategoryId = subcat2.SubCategoryId, ProductName = "Cotton Kurta", BasePrice = 1200, SKU = "CTN-KRT-M", IsActive = true };
            var product3 = new Product { SubCategoryId = subcat3.SubCategoryId, ProductName = "Designer Bridal Lehenga", BasePrice = 85000, SKU = "BDL-LHG-RED", IsActive = true };
            _context.Products.AddRange(product1, product2, product3);
            await _context.SaveChangesAsync();

            var variant1 = new ProductVariant { ProductId = product1.ProductId, VariantSKU = "LNN-SMR-SH-XL", VariantPrice = 1500, IsActive = true };
            var variant2 = new ProductVariant { ProductId = product2.ProductId, VariantSKU = "CTN-KRT-M", VariantPrice = 1200, IsActive = true };
            var variant3 = new ProductVariant { ProductId = product3.ProductId, VariantSKU = "BDL-LHG-RED", VariantPrice = 85000, IsActive = true };
            _context.ProductVariants.AddRange(variant1, variant2, variant3);
            await _context.SaveChangesAsync();

            // Default supplier categories to Linens & Cotton if they are empty
            var suppliersList = await _context.Suppliers.ToListAsync();
            if (suppliersList.Any())
            {
                foreach (var sup in suppliersList)
                {
                    if (string.IsNullOrEmpty(sup.Category))
                    {
                        sup.Category = "Linens & Cotton";
                    }
                }
                await _context.SaveChangesAsync();
            }
        }

        var categories = await _context.Categories
            .Select(c => new {
                id = c.CategoryId,
                name = c.CategoryName
            })
            .ToListAsync();

        return Json(categories);
    }

    // GET: /api/subcategories
    [HttpGet("api/subcategories")]
    public async Task<IActionResult> GetSubCategories()
    {
        var subcategories = await _context.SubCategories
            .Select(sc => new {
                id = sc.SubCategoryId,
                categoryId = sc.CategoryId,
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
