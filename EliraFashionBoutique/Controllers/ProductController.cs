using EliraFashionBoutique.Models;
using EliraFashionBoutique.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace EliraFashionBoutique.Controllers;

public class ProductController : Controller
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ISubCategoryRepository _subCategoryRepository;
    private readonly IColorRepository _colorRepository;
    private readonly ISizeRepository _sizeRepository;
    private readonly IPromotionRepository _promotionRepository;

    public ProductController(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        ISubCategoryRepository subCategoryRepository,
        IColorRepository colorRepository,
        ISizeRepository sizeRepository,
        IPromotionRepository promotionRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _subCategoryRepository = subCategoryRepository;
        _colorRepository = colorRepository;
        _sizeRepository = sizeRepository;
        _promotionRepository = promotionRepository;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        try
        {
            var products = await _productRepository.GetAllAsync();
            var promotions = await _promotionRepository.GetAllAsync();

            var list = products.Select(p => {
                var catName = p.SubCategory?.Category?.CategoryName ?? "Unknown";
                var subCatName = p.SubCategory?.SubcategoryName ?? "Unknown";
                var season = p.SubCategory?.SeasonType ?? "Unknown";
                var start = p.SubCategory?.StartDate?.ToString("yyyy-MM-dd") ?? "N/A";
                var end = p.SubCategory?.EndDate?.ToString("yyyy-MM-dd") ?? "N/A";
                
                // Active promotion on the subcategory
                var activePromo = promotions.FirstOrDefault(pr => pr.SubCategoryId == p.SubCategoryId && (pr.IsActive ?? false));

                return new {
                    id = "PROD-" + p.ProductId,
                    productId = p.ProductId,
                    baseName = p.ProductName,
                    description = p.Description,
                    masterSKU = p.SKU,
                    basePrice = p.BasePrice,
                    categoryId = p.SubCategory?.CategoryId?.ToString() ?? "0",
                    subCategoryId = p.SubCategoryId?.ToString() ?? "0",
                    promotionId = activePromo?.PromotionId.ToString() ?? "",
                    seasonDisplay = $"{season} ({start} to {end})",
                    isActive = p.IsActive ?? false,
                    galleries = p.ProductImages
                        .GroupBy(img => img.ColorId)
                        .Select((g, index) => {
                            var color = g.First().Color;
                            return new {
                                id = "gal_" + (color?.ColorId ?? index),
                                colorId = color?.ColorId ?? 0,
                                colorName = color?.ColorName ?? "No Color",
                                colorHex = color?.HexCode ?? "#FFFFFF",
                                images = g.Select(img => new {
                                    imageId = img.ImageId,
                                    url = img.ImageURL,
                                    isCover = img.IsPrimary ?? false
                                }).ToList()
                            };
                        }).ToList(),
                    variants = p.ProductVariants.Select(v => new {
                        variantId = v.VariantId,
                        sizeId = v.SizeId ?? 0,
                        size = v.Size?.SizeName ?? "Unknown",
                        colorId = v.ColorId ?? 0,
                        color = v.Color?.ColorName ?? "Unknown",
                        price = v.VariantPrice ?? p.BasePrice,
                        weight = v.Weight ?? 0,
                        variantSKU = v.VariantSKU
                    }).ToList()
                };
            });

            return Json(list);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] Product product)
    {
        if (product == null)
        {
            return Json(new { success = false, message = "Invalid data." });
        }

        if (string.IsNullOrWhiteSpace(product.ProductName))
        {
            return Json(new { success = false, message = "Product Name is required." });
        }

        if (string.IsNullOrWhiteSpace(product.SKU))
        {
            return Json(new { success = false, message = "Master SKU is required." });
        }

        try
        {
            // Verify SKU Uniqueness
            var allProducts = await _productRepository.GetAllAsync();
            var isDuplicateSKU = allProducts.Any(p => p.SKU == product.SKU && p.ProductId != product.ProductId);
            if (isDuplicateSKU)
            {
                return Json(new { success = false, message = $"Product SKU '{product.SKU}' already exists in database." });
            }

            if (product.ProductId == 0)
            {
                // CREATE NEW PRODUCT
                product.CreatedAt = DateTime.Now;
                product.ProductVariants = new List<ProductVariant>();
                product.ProductImages = new List<ProductImage>();

                await _productRepository.AddAsync(product);
            }
            else
            {
                // UPDATE EXISTING PRODUCT
                var existing = await _productRepository.GetByIdAsync(product.ProductId);
                if (existing == null)
                {
                    return Json(new { success = false, message = "Product record not found." });
                }

                existing.SubCategoryId = product.SubCategoryId;
                existing.ProductName = product.ProductName;
                existing.Description = product.Description;
                existing.BasePrice = product.BasePrice;
                existing.SKU = product.SKU;
                existing.IsActive = product.IsActive;

                await _productRepository.UpdateAsync(existing);
            }

            return Json(new { success = true, productId = product.ProductId });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _productRepository.DeleteAsync(id);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteByStringId(string id)
    {
        try
        {
            int productId = 0;
            if (id.StartsWith("PROD-"))
            {
                int.TryParse(id.Replace("PROD-", ""), out productId);
            }
            else
            {
                int.TryParse(id, out productId);
            }

            await _productRepository.DeleteAsync(productId);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}
