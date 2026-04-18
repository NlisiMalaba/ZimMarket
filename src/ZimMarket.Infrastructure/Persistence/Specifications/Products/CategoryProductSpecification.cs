using ZimMarket.Domain.Common.Specifications;
using ZimMarket.Domain.Entities.Catalogue;

namespace ZimMarket.Infrastructure.Persistence.Specifications.Products;

internal sealed class CategoryProductSpecification : Specification<Product>
{
    public CategoryProductSpecification(Guid categoryId)
    {
        AddCriteria(product => product.CategoryId == categoryId);
    }
}
