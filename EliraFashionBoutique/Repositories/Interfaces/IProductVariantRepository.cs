using EliraFashionBoutique.Models;

namespace EliraFashionBoutique.Repositories.Interfaces;

public interface IProductVariantRepository
{
    Task<IEnumerable<ProductVariant>> GetAllAsync();
    Task<ProductVariant?> GetByIdAsync(int id);
    Task AddAsync(ProductVariant variant);
    Task UpdateAsync(ProductVariant variant);
    Task DeleteAsync(int id);
    Task<IEnumerable<ProductVariant>> GetByProductIdAsync(int productId);
    Task SyncVariantsAsync(int productId, IEnumerable<ProductVariant> variants);
}

