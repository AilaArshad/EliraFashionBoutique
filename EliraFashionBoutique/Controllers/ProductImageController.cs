using EliraFashionBoutique.Models;
using EliraFashionBoutique.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Authorization;

namespace EliraFashionBoutique.Controllers;

[Authorize(Policy = "AdminAccess")]
public class ProductImageController : Controller
{
    private readonly IProductImageRepository _imageRepository;

    public ProductImageController(IProductImageRepository imageRepository)
    {
        _imageRepository = imageRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetByProduct(int productId)
    {
        try
        {
            var images = await _imageRepository.GetByProductIdAsync(productId);
            return Json(images);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] ProductImage image)
    {
        if (image == null)
        {
            return Json(new { success = false, message = "Invalid image data." });
        }

        try
        {
            await _imageRepository.AddAsync(image);
            return Json(new { success = true, imageId = image.ImageId });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Update([FromBody] ProductImage image)
    {
        if (image == null)
        {
            return Json(new { success = false, message = "Invalid image data." });
        }

        try
        {
            await _imageRepository.UpdateAsync(image);
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
            await _imageRepository.DeleteAsync(id);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> ManagePrimary(int imageId)
    {
        try
        {
            var image = await _imageRepository.GetByIdAsync(imageId);
            if (image == null)
            {
                return Json(new { success = false, message = "Image not found." });
            }

            var allImages = await _imageRepository.GetByProductIdAsync(image.ProductId ?? 0);
            foreach (var img in allImages)
            {
                img.IsPrimary = (img.ImageId == imageId);
                await _imageRepository.UpdateAsync(img);
            }

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Sync(int productId, [FromBody] List<ProductImage> images)
    {
        if (images == null)
        {
            images = new List<ProductImage>();
        }

        try
        {
            await _imageRepository.SyncImagesAsync(productId, images);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}
