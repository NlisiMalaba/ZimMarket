using ZimMarket.Domain.Common.Specifications;
using ZimMarket.Domain.Entities.Catalogue;
using ZimMarket.Domain.Enums;

namespace ZimMarket.Infrastructure.Persistence.Specifications.Products;

/// <summary>Restricts catalogue queries to listings visible on the public marketplace.</summary>
internal sealed class ActiveProductSpecification : Specification<Product>
{
    public ActiveProductSpecification()
    {
        AddCriteria(product => product.Status == ProductStatus.Active);
    }
}
