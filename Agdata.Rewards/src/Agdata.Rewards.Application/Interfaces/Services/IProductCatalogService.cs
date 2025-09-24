using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Application.Interfaces.Services;

public interface IProductCatalogService
{
    Task<Guid> AddNewProductAsync(Admin creator, string name, int requiredPoints, int? stock);
    Task UpdateProductDetailsAsync(Admin editor, Guid productId, string name, int requiredPoints, int? stock);
    Task DeactivateProductAsync(Admin editor, Guid productId);
    Task ActivateProductAsync(Admin editor, Guid productId);
    Task DeleteProductAsync(Admin deleter, Guid productId);
    Task<IEnumerable<Product>> GetFullCatalogAsync();
}