using EliraFashionBoutique.Models;
using EliraFashionBoutique.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EliraFashionBoutique.Controllers;

public class SubCategoryController : Controller
{
    private readonly ISubCategoryRepository _subCategoryRepository;

    public SubCategoryController(ISubCategoryRepository subCategoryRepository)
    {
        _subCategoryRepository = subCategoryRepository;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var subCategories = await _subCategoryRepository.GetAllAsync();
        var list = subCategories.Select(s => new {
            s.SubCategoryId,
            s.CategoryId,
            CategoryName = s.Category?.CategoryName ?? "N/A",
            s.SubcategoryName,
            s.SeasonType,
            StartDate = s.StartDate?.ToString("yyyy-MM-dd"),
            EndDate = s.EndDate?.ToString("yyyy-MM-dd"),
            s.IsActive
        });
        return Json(list);
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] SubCategory subCategory)
    {
        if (subCategory == null)
        {
            return Json(new { success = false, message = "Invalid data." });
        }

        if (string.IsNullOrWhiteSpace(subCategory.SubcategoryName))
        {
            return Json(new { success = false, message = "Sub-Category Name is required." });
        }

        if (string.IsNullOrWhiteSpace(subCategory.SeasonType))
        {
            return Json(new { success = false, message = "Season Type is required." });
        }

        if (subCategory.CategoryId == null || subCategory.CategoryId <= 0)
        {
            return Json(new { success = false, message = "Parent Category is required." });
        }

        try
        {
            if (subCategory.SubCategoryId == 0)
            {
                await _subCategoryRepository.AddAsync(subCategory);
            }
            else
            {
                var existing = await _subCategoryRepository.GetByIdAsync(subCategory.SubCategoryId);
                if (existing == null)
                {
                    return Json(new { success = false, message = "Sub-Category not found." });
                }
                existing.CategoryId = subCategory.CategoryId;
                existing.SubcategoryName = subCategory.SubcategoryName;
                existing.SeasonType = subCategory.SeasonType;
                existing.StartDate = subCategory.StartDate;
                existing.EndDate = subCategory.EndDate;
                existing.IsActive = subCategory.IsActive;
                await _subCategoryRepository.UpdateAsync(existing);
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
            await _subCategoryRepository.DeleteAsync(id);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}
