using System.Text.RegularExpressions;
using ZimMarket.Shared;

namespace ZimMarket.Domain.Entities.Catalogue;

public sealed partial class Category : BaseEntity
{
    public const int MaxNameLength = 200;
    public const int MaxSlugLength = 150;

    private Category()
    {
    }

    public string Name { get; private set; } = null!;

    public string Slug { get; private set; } = null!;

    public Guid? ParentCategoryId { get; private set; }

    public bool IsRoot => ParentCategoryId is null;

    public static Result<Category> Create(
        Guid id,
        string name,
        string slug,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        Category? parentCategory = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<Category>.Failure("Name is required.");

        var trimmedName = name.Trim();
        if (trimmedName.Length > MaxNameLength)
            return Result<Category>.Failure($"Name cannot exceed {MaxNameLength} characters.");

        if (string.IsNullOrWhiteSpace(slug))
            return Result<Category>.Failure("Slug is required.");

        var trimmedSlug = slug.Trim();
        if (trimmedSlug.Length > MaxSlugLength)
            return Result<Category>.Failure($"Slug cannot exceed {MaxSlugLength} characters.");

        var normalizedSlug = trimmedSlug.ToLowerInvariant();
        if (!SlugPattern().IsMatch(normalizedSlug))
            return Result<Category>.Failure(
                "Slug must contain only lowercase letters, digits, and hyphens (e.g. electronics, phone-cases).");

        if (parentCategory != null && parentCategory.ParentCategoryId != null)
            return Result<Category>.Failure("Categories support at most two levels (a subcategory cannot have children).");

        var category = new Category
        {
            Id = id,
            Name = trimmedName,
            Slug = normalizedSlug,
            ParentCategoryId = parentCategory?.Id,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        return Result<Category>.Success(category);
    }

    [GeneratedRegex(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();
}
