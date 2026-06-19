using EliraFashionBoutique.Models;
using EliraFashionBoutique.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EliraFashionBoutique.Repositories.Implementations;

public class ColorRepository : IColorRepository
{
    private readonly EliraDbContext _context;

    public ColorRepository(EliraDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Color>> GetAllAsync()
    {
        return await _context.Colors.ToListAsync();
    }

    public async Task<Color?> GetByIdAsync(int id)
    {
        return await _context.Colors.FindAsync(id);
    }

    public async Task AddAsync(Color color)
    {
        await _context.Colors.AddAsync(color);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Color color)
    {
        _context.Entry(color).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var color = await GetByIdAsync(id);
        if (color != null)
        {
            _context.Colors.Remove(color);
            await _context.SaveChangesAsync();
        }
    }
}
