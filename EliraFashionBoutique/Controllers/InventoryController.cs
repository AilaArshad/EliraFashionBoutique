using EliraFashionBoutique.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace EliraFashionBoutique.Controllers;

[Authorize(Policy = "AdminAccess")]
public class InventoryController : Controller
{
    private readonly EliraDbContext _context;

    public InventoryController(EliraDbContext context)
    {
        _context = context;
    }

    // GET: /Inventory
    public IActionResult Index()
    {
        return View();
    }

    // GET: /api/inventory
    [HttpGet("api/inventory")]
    public async Task<IActionResult> GetInventory()
    {
        try
        {
            var products = await _context.Products
                .Include(p => p.ProductVariants)
                .ToListAsync();

            var inventories = await _context.Inventories.ToListAsync();

            var list = products.Select(p => {
                int cumulativeStock = 0;
                bool triggerReorderWarning = false;
                bool isCompletelyEmpty = p.ProductVariants.Count == 0;

                foreach (var v in p.ProductVariants)
                {
                    var inv = inventories.FirstOrDefault(i => i.VariantId == v.VariantId);
                    int stock = inv?.QuantityAvailable ?? 0;
                    cumulativeStock += stock;

                    int threshold = inv?.ReorderLevel ?? 10;

                    if (stock <= threshold)
                    {
                        triggerReorderWarning = true;
                    }
                }

                return new {
                    id = p.ProductId,
                    name = p.ProductName,
                    sku = p.SKU ?? "ELR-BASE-" + p.ProductId,
                    description = p.Description ?? "No description",
                    cumulativeStock = cumulativeStock,
                    triggerReorderWarning = triggerReorderWarning,
                    isOutOfStock = isCompletelyEmpty || (p.ProductVariants.Count > 0 && cumulativeStock == 0)
                };
            }).ToList();

            return Json(list);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { detail = ex.Message });
        }
    }

    // GET: /api/inventory/{productId}/variants
    [HttpGet("api/inventory/{productId}/variants")]
    public async Task<IActionResult> GetVariantsBreakdown(int productId)
    {
        try
        {
            var product = await _context.Products
                .Include(p => p.ProductVariants)
                    .ThenInclude(v => v.Size)
                .Include(p => p.ProductVariants)
                    .ThenInclude(v => v.Color)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
            {
                return NotFound(new { detail = "Product not found." });
            }

            var inventories = await _context.Inventories.ToListAsync();

            var list = product.ProductVariants.Select(v => {
                var inv = inventories.FirstOrDefault(i => i.VariantId == v.VariantId);
                int stock = inv?.QuantityAvailable ?? 0;
                int threshold = inv?.ReorderLevel ?? 10;

                string sku = v.VariantSKU ?? ("Variant-" + v.VariantId);

                return new {
                    variantId = v.VariantId,
                    sku = sku,
                    size = v.Size?.SizeName ?? "N/A",
                    colorName = v.Color?.ColorName ?? "N/A",
                    hex = v.Color?.HexCode ?? "#FFFFFF",
                    quantityAvailable = stock,
                    reorderLevel = threshold,
                    lastUpdated = DateTime.Now.ToString("yyyy-MM-dd hh:mm tt")
                };
            }).ToList();

            return Json(list);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { detail = ex.Message });
        }
    }

    // POST: /api/inventory/threshold
    [HttpPost("api/inventory/threshold")]
    public async Task<IActionResult> SaveThresholdLimit([FromBody] ThresholdInputDto input)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.Sku))
        {
            return BadRequest(new { detail = "Invalid SKU or threshold value." });
        }

        try
        {
            var variant = await _context.ProductVariants.FirstOrDefaultAsync(v => v.VariantSKU == input.Sku);
            if (variant == null)
            {
                return NotFound(new { detail = "Variant not found." });
            }

            var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.VariantId == variant.VariantId);
            if (inventory == null)
            {
                inventory = new Inventory
                {
                    VariantId = variant.VariantId,
                    QuantityAvailable = 0,
                    ReorderLevel = input.Threshold
                };
                _context.Inventories.Add(inventory);
            }
            else
            {
                inventory.ReorderLevel = input.Threshold;
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Threshold safety boundary limit updated." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { detail = ex.Message });
        }
    }
}

public class ThresholdInputDto
{
    public string Sku { get; set; } = string.Empty;
    public int Threshold { get; set; }
}
