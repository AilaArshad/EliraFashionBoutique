using EliraFashionBoutique.Models;
using EliraFashionBoutique.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Authorization;

namespace EliraFashionBoutique.Controllers;

[Authorize(Policy = "AdminAccess")]
public class ProductVariantController : Controller
{
    private readonly IProductVariantRepository _variantRepository;

    public ProductVariantController(IProductVariantRepository variantRepository)
    {
        _variantRepository = variantRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetByProduct(int productId)
    {
        try
        {
            var variants = await _variantRepository.GetByProductIdAsync(productId);
            return Json(variants);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] ProductVariant variant)
    {
        if (variant == null)
        {
            return Json(new { success = false, message = "Invalid variant data." });
        }

        try
        {
            await _variantRepository.AddAsync(variant);
            return Json(new { success = true, variantId = variant.VariantId });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Update([FromBody] ProductVariant variant)
    {
        if (variant == null)
        {
            return Json(new { success = false, message = "Invalid variant data." });
        }

        try
        {
            await _variantRepository.UpdateAsync(variant);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _variantRepository.DeleteAsync(id);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Sync(int productId, [FromBody] List<ProductVariant> variants)
    {
        if (variants == null)
        {
            variants = new List<ProductVariant>();
        }

        try
        {
            await _variantRepository.SyncVariantsAsync(productId, variants);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}
