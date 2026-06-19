using EliraFashionBoutique.Models;
using EliraFashionBoutique.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EliraFashionBoutique.Repositories.Implementations;

public class PromotionRepository : IPromotionRepository
{
    private readonly EliraDbContext _context;

    public PromotionRepository(EliraDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Promotion>> GetAllAsync()
    {
        return await _context.Promotions.Include(p => p.SubCategory).ToListAsync();
    }

    public async Task<Promotion?> GetByIdAsync(int id)
    {
        return await _context.Promotions.FindAsync(id);
    }

    public async Task AddAsync(Promotion promotion)
    {
        await _context.Promotions.AddAsync(promotion);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Promotion promotion)
    {
        _context.Entry(promotion).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var promotion = await GetByIdAsync(id);
        if (promotion != null)
        {
            _context.Promotions.Remove(promotion);
            await _context.SaveChangesAsync();
        }
    }
}
