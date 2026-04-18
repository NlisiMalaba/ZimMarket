using MediatR;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Application.Catalogue;

public sealed class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, Result<IReadOnlyList<CategoryDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCategoriesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<IReadOnlyList<CategoryDto>>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await _unitOfWork.Categories.GetAllAsync(cancellationToken).ConfigureAwait(false);

        IReadOnlyList<CategoryDto> items = categories
            .Select(category => new CategoryDto
            {
                CategoryId = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                ParentCategoryId = category.ParentCategoryId
            })
            .ToList();

        return Result<IReadOnlyList<CategoryDto>>.Success(items);
    }
}
