namespace ZimMarket.Application.Catalogue;

public sealed class CategoryDto
{
    public required Guid CategoryId { get; init; }

    public required string Name { get; init; }

    public required string Slug { get; init; }

    public Guid? ParentCategoryId { get; init; }
}
