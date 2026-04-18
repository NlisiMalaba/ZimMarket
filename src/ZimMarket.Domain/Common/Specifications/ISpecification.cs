using System.Linq.Expressions;

namespace ZimMarket.Domain.Common.Specifications;

public interface ISpecification<T>
{
    IReadOnlyList<Expression<Func<T, bool>>> Criteria { get; }
}
