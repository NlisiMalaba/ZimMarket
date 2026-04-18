using ZimMarket.Domain.Common.Specifications;
using ZimMarket.Domain.Entities.Catalogue;

namespace ZimMarket.Infrastructure.Persistence.Specifications.Products;

internal sealed class SellerProductSpecification : Specification<Product>
{
    public SellerProductSpecification(Guid sellerId)
    {
        AddCriteria(product => product.SellerId == sellerId);
    }
}
