using EliraFashionBoutique.Models;
using EliraFashionBoutique.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace EliraFashionBoutique.Controllers;

[Authorize]
public class CategoryController : Controller
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryController(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var categories = await _categoryRepository.GetAllAsync();
        var list = categories.Select(c => new {
            c.CategoryId,
            c.CategoryName,
            c.Description
        });
        return Json(list);
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] Category category)
    {
        if (category == null)
        {
            return Json(new { success = false, message = "Invalid data." });
        }

        if (string.IsNullOrWhiteSpace(category.CategoryName))
        {
            return Json(new { success = false, message = "Category Name is required." });
        }

        try
        {
            if (category.CategoryId == 0)
            {
                await _categoryRepository.AddAsync(category);
            }
            else
            {
                var existing = await _categoryRepository.GetByIdAsync(category.CategoryId);
                if (existing == null)
                {
                    return Json(new { success = false, message = "Category not found." });
                }
                existing.CategoryName = category.CategoryName;
                existing.Description = category.Description;
                await _categoryRepository.UpdateAsync(existing);
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
            await _categoryRepository.DeleteAsync(id);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}
