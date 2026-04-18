using Microsoft.EntityFrameworkCore;
using ZimMarket.Domain.Common.Specifications;
using ZimMarket.Domain.Entities.Catalogue;

namespace ZimMarket.Infrastructure.Persistence.Specifications.Products;

internal sealed class SearchTermProductSpecification : Specification<Product>
{
    public SearchTermProductSpecification(string searchTerm)
    {
        string normalizedTerm = searchTerm.Trim();
        string pattern = $"%{normalizedTerm}%";

        AddCriteria(product =>
            EF.Functions.ToTsVector("english", product.Title + " " + product.Description)
                .Matches(EF.Functions.PlainToTsQuery("english", normalizedTerm)) ||
            EF.Functions.ILike(product.Title, pattern) ||
            EF.Functions.ILike(product.Description, pattern));
    }
}
