using EliraFashionBoutique.Models;
using EliraFashionBoutique.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EliraFashionBoutique.Repositories.Implementations;

public class SubCategoryRepository : ISubCategoryRepository
{
    private readonly EliraDbContext _context;

    public SubCategoryRepository(EliraDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<SubCategory>> GetAllAsync()
    {
        return await _context.SubCategories.Include(s => s.Category).ToListAsync();
    }

    public async Task<SubCategory?> GetByIdAsync(int id)
    {
        return await _context.SubCategories.FindAsync(id);
    }

    public async Task AddAsync(SubCategory subCategory)
    {
        await _context.SubCategories.AddAsync(subCategory);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(SubCategory subCategory)
    {
        _context.Entry(subCategory).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var subCategory = await GetByIdAsync(id);
        if (subCategory != null)
        {
            _context.SubCategories.Remove(subCategory);
            await _context.SaveChangesAsync();
        }
    }
}
