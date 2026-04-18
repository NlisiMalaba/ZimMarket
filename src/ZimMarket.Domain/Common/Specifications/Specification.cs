using System.Linq.Expressions;

namespace ZimMarket.Domain.Common.Specifications;

public abstract class Specification<T> : ISpecification<T>
{
    private readonly List<Expression<Func<T, bool>>> _criteria = [];

    public IReadOnlyList<Expression<Func<T, bool>>> Criteria => _criteria;

    protected void AddCriteria(Expression<Func<T, bool>> criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        _criteria.Add(criteria);
    }
}
