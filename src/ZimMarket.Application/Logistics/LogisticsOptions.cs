namespace ZimMarket.Application.Logistics;

public sealed class LogisticsOptions
{
    public const string SectionName = "Logistics";

    /// <summary>Logical warehouse used for pickup until a dedicated warehouse registry exists.</summary>
    public Guid DefaultPickupWarehouseId { get; set; } = Guid.Parse("d0000000-0000-4000-8000-000000000001");
}
