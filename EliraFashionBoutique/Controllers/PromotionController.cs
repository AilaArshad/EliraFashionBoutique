using EliraFashionBoutique.Models;
using EliraFashionBoutique.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EliraFashionBoutique.Controllers;

public class PromotionController : Controller
{
    private readonly IPromotionRepository _promotionRepository;

    public PromotionController(IPromotionRepository promotionRepository)
    {
        _promotionRepository = promotionRepository;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var promotions = await _promotionRepository.GetAllAsync();
        var list = promotions.Select(p => new {
            p.PromotionId,
            p.SubCategoryId,
            SubcategoryName = p.SubCategory?.SubcategoryName ?? "N/A",
            p.PromotionDiscount,
            p.DiscountName,
            p.DiscountType,
            StartDate = p.StartDate?.ToString("yyyy-MM-dd"),
            EndDate = p.EndDate?.ToString("yyyy-MM-dd"),
            p.IsActive
        });
        return Json(list);
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] Promotion promotion)
    {
        if (promotion == null)
        {
            return Json(new { success = false, message = "Invalid data." });
        }

        if (string.IsNullOrWhiteSpace(promotion.DiscountName))
        {
            return Json(new { success = false, message = "Promotion Name is required." });
        }

        if (promotion.SubCategoryId == null || promotion.SubCategoryId <= 0)
        {
            return Json(new { success = false, message = "Target Sub-Category is required." });
        }

        try
        {
            if (promotion.PromotionId == 0)
            {
                await _promotionRepository.AddAsync(promotion);
            }
            else
            {
                var existing = await _promotionRepository.GetByIdAsync(promotion.PromotionId);
                if (existing == null)
                {
                    return Json(new { success = false, message = "Promotion not found." });
                }
                existing.SubCategoryId = promotion.SubCategoryId;
                existing.PromotionDiscount = promotion.PromotionDiscount;
                existing.DiscountName = promotion.DiscountName;
                existing.DiscountType = promotion.DiscountType;
                existing.StartDate = promotion.StartDate;
                existing.EndDate = promotion.EndDate;
                existing.IsActive = promotion.IsActive;
                await _promotionRepository.UpdateAsync(existing);
            }
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
            await _promotionRepository.DeleteAsync(id);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}
