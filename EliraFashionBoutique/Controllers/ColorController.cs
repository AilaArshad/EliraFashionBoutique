using EliraFashionBoutique.Models;
using EliraFashionBoutique.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EliraFashionBoutique.Controllers;

public class ColorController : Controller
{
    private readonly IColorRepository _colorRepository;

    public ColorController(IColorRepository colorRepository)
    {
        _colorRepository = colorRepository;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var colors = await _colorRepository.GetAllAsync();
        var list = colors.Select(c => new {
            c.ColorId,
            c.ColorName,
            c.HexCode
        });
        return Json(list);
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] Color color)
    {
        if (color == null)
        {
            return Json(new { success = false, message = "Invalid data." });
        }

        if (string.IsNullOrWhiteSpace(color.ColorName))
        {
            return Json(new { success = false, message = "Color Name is required." });
        }

        try
        {
            if (color.ColorId == 0)
            {
                await _colorRepository.AddAsync(color);
            }
            else
            {
                var existing = await _colorRepository.GetByIdAsync(color.ColorId);
                if (existing == null)
                {
                    return Json(new { success = false, message = "Color not found." });
                }
                existing.ColorName = color.ColorName;
                existing.HexCode = color.HexCode;
                await _colorRepository.UpdateAsync(existing);
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
            await _colorRepository.DeleteAsync(id);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}
