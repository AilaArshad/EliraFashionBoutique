using EliraFashionBoutique.Models;
using EliraFashionBoutique.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EliraFashionBoutique.Repositories.Implementations;

public class SizeRepository : ISizeRepository
{
    private readonly EliraDbContext _context;

    public SizeRepository(EliraDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Size>> GetAllAsync()
    {
        return await _context.Sizes.ToListAsync();
    }

    public async Task<Size?> GetByIdAsync(int id)
    {
        return await _context.Sizes.FindAsync(id);
    }

    public async Task AddAsync(Size size)
    {
        await _context.Sizes.AddAsync(size);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Size size)
    {
        _context.Entry(size).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var size = await GetByIdAsync(id);
        if (size != null)
        {
            _context.Sizes.Remove(size);
            await _context.SaveChangesAsync();
        }
    }
}
