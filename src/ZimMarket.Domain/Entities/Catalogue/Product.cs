using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Events;
using ZimMarket.Domain.Exceptions;
using ZimMarket.Domain.ValueObjects;
using ZimMarket.Shared;

namespace ZimMarket.Domain.Entities.Catalogue;

public sealed class Product : BaseEntity
{
    public const int MaxImageKeys = 5;
    public const int MaxTitleLength = 300;
    public const int MaxDescriptionLength = 8000;

    private readonly List<string> _imageKeys = [];

    private Product()
    {
    }

    public Guid SellerId { get; private set; }

    public string Title { get; private set; } = null!;

    public string Description { get; private set; } = null!;

    public Money Price { get; private set; } = null!;

    public Guid CategoryId { get; private set; }

    public int StockQuantity { get; private set; }

    public IReadOnlyList<string> ImageKeys => _imageKeys;

    public ProductStatus Status { get; private set; }

    public Address PickupAddress { get; private set; } = null!;

    public static Result<Product> Create(
        Guid id,
        Guid sellerId,
        string title,
        string description,
        Money price,
        Guid categoryId,
        int stockQuantity,
        IReadOnlyList<string> imageKeys,
        Address pickupAddress,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        var titleError = ValidateTitle(title);
        if (titleError != null)
            return Result<Product>.Failure(titleError);

        var descError = ValidateDescription(description);
        if (descError != null)
            return Result<Product>.Failure(descError);

        ArgumentNullException.ThrowIfNull(price);
        ArgumentNullException.ThrowIfNull(pickupAddress);

        if (stockQuantity < 0)
            return Result<Product>.Failure("Stock quantity cannot be negative.");

        var keysError = ValidateImageKeys(imageKeys);
        if (keysError != null)
            return Result<Product>.Failure(keysError);

        var product = new Product
        {
            Id = id,
            SellerId = sellerId,
            Title = title.Trim(),
            Description = description.Trim(),
            Price = price,
            CategoryId = categoryId,
            StockQuantity = stockQuantity,
            Status = ProductStatus.Active,
            PickupAddress = pickupAddress,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        product._imageKeys.AddRange(imageKeys.Select(k => k.Trim()));

        return Result<Product>.Success(product);
    }

    public void UpdateDetails(
        string title,
        string description,
        Money price,
        Guid categoryId,
        IReadOnlyList<string> imageKeys,
        Address pickupAddress)
    {
        EnsureCanModify();

        var titleError = ValidateTitle(title);
        if (titleError != null)
            throw new DomainException(titleError);

        var descError = ValidateDescription(description);
        if (descError != null)
            throw new DomainException(descError);

        ArgumentNullException.ThrowIfNull(price);
        ArgumentNullException.ThrowIfNull(pickupAddress);

        var keysError = ValidateImageKeys(imageKeys);
        if (keysError != null)
            throw new DomainException(keysError);

        Title = title.Trim();
        Description = description.Trim();
        Price = price;
        CategoryId = categoryId;
        PickupAddress = pickupAddress;
        _imageKeys.Clear();
        _imageKeys.AddRange(imageKeys.Select(k => k.Trim()));
        Touch();
    }

    public void UpdateStock(int delta)
    {
        EnsureCanModify();

        var next = StockQuantity + delta;
        if (next < 0)
            throw new DomainException("Stock quantity cannot be negative.");

        var previous = StockQuantity;
        StockQuantity = next;
        Touch();

        if (previous > 0 && next == 0)
            AddDomainEvent(new StockDepletedEvent(Id));
    }

    public void Suspend()
    {
        if (Status == ProductStatus.Deleted)
            throw new DomainException("Cannot suspend a deleted product.");

        if (Status == ProductStatus.Suspended)
            return;

        Status = ProductStatus.Suspended;
        Touch();
    }

    public void Restore()
    {
        if (Status == ProductStatus.Deleted)
            throw new DomainException("Cannot restore a deleted product.");

        if (Status == ProductStatus.Active)
            return;

        Status = ProductStatus.Active;
        Touch();
    }

    public void Delete()
    {
        if (Status == ProductStatus.Deleted)
            return;

        Status = ProductStatus.Deleted;
        Touch();
    }

    private void EnsureCanModify()
    {
        if (Status == ProductStatus.Deleted)
            throw new DomainException("Cannot modify a deleted product.");
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;

    private static string? ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "Title is required.";

        if (title.Trim().Length > MaxTitleLength)
            return $"Title cannot exceed {MaxTitleLength} characters.";

        return null;
    }

    private static string? ValidateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return "Description is required.";

        if (description.Trim().Length > MaxDescriptionLength)
            return $"Description cannot exceed {MaxDescriptionLength} characters.";

        return null;
    }

    private static string? ValidateImageKeys(IReadOnlyList<string> imageKeys)
    {
        ArgumentNullException.ThrowIfNull(imageKeys);

        if (imageKeys.Count > MaxImageKeys)
            return $"A product can have at most {MaxImageKeys} images.";

        foreach (var key in imageKeys)
        {
            if (string.IsNullOrWhiteSpace(key))
                return "Image keys cannot be empty.";
        }

        return null;
    }
}
