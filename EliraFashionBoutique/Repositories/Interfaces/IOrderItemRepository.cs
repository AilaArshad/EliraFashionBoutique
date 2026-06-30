using EliraFashionBoutique.Models;

namespace EliraFashionBoutique.Repositories.Interfaces;

public interface IOrderItemRepository
{
    Task<IEnumerable<OrderItem>> GetAllAsync();
    Task<OrderItem?> GetByIdAsync(int id);
    Task<IEnumerable<OrderItem>> GetByOrderIdAsync(int orderId);
    Task AddAsync(OrderItem orderItem);
    Task UpdateAsync(OrderItem orderItem);
    Task DeleteAsync(int id);
}
