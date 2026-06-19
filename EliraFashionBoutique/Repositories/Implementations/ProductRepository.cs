using EliraFashionBoutique.Models;
using EliraFashionBoutique.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EliraFashionBoutique.Repositories.Implementations;

public class ProductRepository : IProductRepository
{
    private readonly EliraDbContext _context;

    public ProductRepository(EliraDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        return await _context.Products
            .Include(p => p.SubCategory)
                .ThenInclude(s => s!.Category)
            .Include(p => p.ProductVariants)
                .ThenInclude(v => v.Size)
            .Include(p => p.ProductVariants)
                .ThenInclude(v => v.Color)
            .Include(p => p.ProductImages)
                .ThenInclude(i => i.Color)
            .ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _context.Products
            .Include(p => p.SubCategory)
            .Include(p => p.ProductVariants)
            .Include(p => p.ProductImages)
            .FirstOrDefaultAsync(p => p.ProductId == id);
    }

    public async Task AddAsync(Product product)
    {
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Product product)
    {
        _context.Entry(product).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var product = await GetByIdAsync(id);
        if (product != null)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }
    }
}
