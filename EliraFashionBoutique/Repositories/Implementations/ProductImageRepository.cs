using EliraFashionBoutique.Models;
using EliraFashionBoutique.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EliraFashionBoutique.Repositories.Implementations;

public class ProductImageRepository : IProductImageRepository
{
    private readonly EliraDbContext _context;

    public ProductImageRepository(EliraDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ProductImage>> GetAllAsync()
    {
        return await _context.ProductImages
            .Include(i => i.Color)
            .ToListAsync();
    }

    public async Task<ProductImage?> GetByIdAsync(int id)
    {
        return await _context.ProductImages
            .Include(i => i.Color)
            .FirstOrDefaultAsync(i => i.ImageId == id);
    }

    public async Task AddAsync(ProductImage productImage)
    {
        await _context.ProductImages.AddAsync(productImage);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ProductImage productImage)
    {
        _context.Entry(productImage).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var img = await GetByIdAsync(id);
        if (img != null)
        {
            _context.ProductImages.Remove(img);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<ProductImage>> GetByProductIdAsync(int productId)
    {
        return await _context.ProductImages
            .Include(i => i.Color)
            .Where(i => i.ProductId == productId)
            .ToListAsync();
    }

    public async Task SyncImagesAsync(int productId, IEnumerable<ProductImage> images)
    {
        // 1. Load existing images
        var existing = await _context.ProductImages
            .Where(i => i.ProductId == productId)
            .ToListAsync();

        // 2. Delete images that are in database but not in incoming list
        var toDelete = existing
            .Where(e => !images.Any(i => i.ImageId == e.ImageId && e.ImageId != 0))
            .ToList();

        if (toDelete.Any())
        {
            _context.ProductImages.RemoveRange(toDelete);
            await _context.SaveChangesAsync(); // Flush deletes first
        }

        // 3. Update existing or insert new
        foreach (var incoming in images)
        {
            if (incoming.ImageId != 0)
            {
                var dbImg = existing.FirstOrDefault(e => e.ImageId == incoming.ImageId);
                if (dbImg != null)
                {
                    dbImg.ColorId = incoming.ColorId;
                    dbImg.ImageURL = incoming.ImageURL;
                    dbImg.IsPrimary = incoming.IsPrimary;
                    dbImg.DisplayOrder = incoming.DisplayOrder;
                    _context.Entry(dbImg).State = EntityState.Modified;
                }
            }
            else
            {
                incoming.ProductId = productId;
                incoming.ImageId = 0; // ensure EF treats it as new
                await _context.ProductImages.AddAsync(incoming);
            }
        }

        await _context.SaveChangesAsync();
    }
}
