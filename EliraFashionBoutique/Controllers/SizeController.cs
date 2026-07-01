using EliraFashionBoutique.Models;
using EliraFashionBoutique.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace EliraFashionBoutique.Controllers;

[Authorize(Policy = "AdminAccess")]
public class SizeController : Controller
{
    private readonly ISizeRepository _sizeRepository;

    public SizeController(ISizeRepository sizeRepository)
    {
        _sizeRepository = sizeRepository;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var sizes = await _sizeRepository.GetAllAsync();
        var list = sizes.Select(s => new {
            s.SizeId,
            s.SizeName
        });
        return Json(list);
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] Size size)
    {
        if (size == null)
        {
            return Json(new { success = false, message = "Invalid data." });
        }

        if (string.IsNullOrWhiteSpace(size.SizeName))
        {
            return Json(new { success = false, message = "Size Name is required." });
        }

        try
        {
            if (size.SizeId == 0)
            {
                await _sizeRepository.AddAsync(size);
            }
            else
            {
                var existing = await _sizeRepository.GetByIdAsync(size.SizeId);
                if (existing == null)
                {
                    return Json(new { success = false, message = "Size not found." });
                }
                existing.SizeName = size.SizeName;
                await _sizeRepository.UpdateAsync(existing);
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
            await _sizeRepository.DeleteAsync(id);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}
