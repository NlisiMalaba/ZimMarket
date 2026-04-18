using ZimMarket.Domain.Common.Specifications;
using ZimMarket.Domain.Entities.Catalogue;

namespace ZimMarket.Infrastructure.Persistence.Specifications.Products;

internal sealed class MinPriceProductSpecification : Specification<Product>
{
    public MinPriceProductSpecification(decimal minPriceUsd)
    {
        AddCriteria(product => product.Price.Amount >= minPriceUsd);
    }
}
