using ZimMarket.Domain.Common.Specifications;
using ZimMarket.Domain.Entities.Catalogue;

namespace ZimMarket.Infrastructure.Persistence.Specifications.Products;

internal sealed class MaxPriceProductSpecification : Specification<Product>
{
    public MaxPriceProductSpecification(decimal maxPriceUsd)
    {
        AddCriteria(product => product.Price.Amount <= maxPriceUsd);
    }
}
