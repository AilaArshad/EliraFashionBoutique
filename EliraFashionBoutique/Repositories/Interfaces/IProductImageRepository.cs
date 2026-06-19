using EliraFashionBoutique.Models;

namespace EliraFashionBoutique.Repositories.Interfaces;

public interface IProductImageRepository
{
    Task<IEnumerable<ProductImage>> GetAllAsync();
    Task<ProductImage?> GetByIdAsync(int id);
    Task AddAsync(ProductImage productImage);
    Task UpdateAsync(ProductImage productImage);
    Task DeleteAsync(int id);
    Task<IEnumerable<ProductImage>> GetByProductIdAsync(int productId);
    Task SyncImagesAsync(int productId, IEnumerable<ProductImage> images);
}
