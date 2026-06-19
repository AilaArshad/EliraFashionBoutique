using EliraFashionBoutique.Models;
using EliraFashionBoutique.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EliraFashionBoutique.Repositories.Implementations;

public class ProductVariantRepository : IProductVariantRepository
{
    private readonly EliraDbContext _context;

    public ProductVariantRepository(EliraDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ProductVariant>> GetAllAsync()
    {
        return await _context.ProductVariants
            .Include(v => v.Size)
            .Include(v => v.Color)
            .ToListAsync();
    }

    public async Task<ProductVariant?> GetByIdAsync(int id)
    {
        return await _context.ProductVariants
            .Include(v => v.Size)
            .Include(v => v.Color)
            .FirstOrDefaultAsync(v => v.VariantId == id);
    }

    public async Task AddAsync(ProductVariant variant)
    {
        await _context.ProductVariants.AddAsync(variant);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ProductVariant variant)
    {
        _context.Entry(variant).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var variant = await GetByIdAsync(id);
        if (variant != null)
        {
            _context.ProductVariants.Remove(variant);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<ProductVariant>> GetByProductIdAsync(int productId)
    {
        return await _context.ProductVariants
            .Include(v => v.Size)
            .Include(v => v.Color)
            .Where(v => v.ProductId == productId)
            .ToListAsync();
    }

    public async Task SyncVariantsAsync(int productId, IEnumerable<ProductVariant> variants)
    {
        // 1. Validate uniqueness in incoming collection
        var compositeKeys = new HashSet<(int? sizeId, int? colorId)>();
        foreach (var v in variants)
        {
            var key = (v.SizeId, v.ColorId);
            if (compositeKeys.Contains(key))
            {
                throw new InvalidOperationException($"Duplicate Variant combination of Size and Color is not allowed for the same product.");
            }
            compositeKeys.Add(key);
        }

        // 2. Load existing variants
        var existing = await _context.ProductVariants
            .Where(v => v.ProductId == productId)
            .ToListAsync();

        // 3. Delete variants that are in database but not in incoming list
        var toDelete = existing
            .Where(e => !variants.Any(i => i.VariantId == e.VariantId && e.VariantId != 0))
            .ToList();

        if (toDelete.Any())
        {
            _context.ProductVariants.RemoveRange(toDelete);
            await _context.SaveChangesAsync(); // Flush deletes first to clear unique index constraints
        }

        // 4. Update existing or insert new
        foreach (var incoming in variants)
        {
            if (incoming.VariantId != 0)
            {
                var dbVar = existing.FirstOrDefault(e => e.VariantId == incoming.VariantId);
                if (dbVar != null)
                {
                    dbVar.SizeId = incoming.SizeId;
                    dbVar.ColorId = incoming.ColorId;
                    dbVar.VariantPrice = incoming.VariantPrice;
                    dbVar.Weight = incoming.Weight;
                    dbVar.VariantSKU = incoming.VariantSKU;
                    dbVar.IsActive = incoming.IsActive;
                    _context.Entry(dbVar).State = EntityState.Modified;
                }
            }
            else
            {
                incoming.ProductId = productId;
                incoming.VariantId = 0; // ensure EF treats it as new
                await _context.ProductVariants.AddAsync(incoming);
            }
        }

        await _context.SaveChangesAsync();
    }
}
