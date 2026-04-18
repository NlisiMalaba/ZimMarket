using System.Linq.Expressions;

namespace ZimMarket.Domain.Common.Specifications;

public sealed class CompositeSpecification<T> : Specification<T>
{
    public CompositeSpecification(IEnumerable<ISpecification<T>> specifications)
    {
        ArgumentNullException.ThrowIfNull(specifications);

        foreach (ISpecification<T> specification in specifications)
        {
            foreach (Expression<Func<T, bool>> criteria in specification.Criteria)
                AddCriteria(criteria);
        }
    }
}
