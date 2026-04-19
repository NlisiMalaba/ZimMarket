using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZimMarket.API.Http;
using ZimMarket.Application.Auth;
using ZimMarket.Application.Drivers;
using ZimMarket.Application.Logistics;
using ZimMarket.Application.Warehouse;
using ZimMarket.Domain.Enums;

namespace ZimMarket.API.Controllers.V1;

[ApiController]
[Route("api/v1/warehouse")]
[Authorize(Policy = AuthorizationPolicies.AdminOrAbove)]
public sealed class WarehouseController : ControllerBase
{
    private readonly ISender _sender;

    public WarehouseController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    [HttpPost("arrivals")]
    public async Task<IActionResult> RecordArrival(
        [FromBody] RecordItemArrivalRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RecordItemArrivalCommand(request.OrderId, request.Notes);
        return (await _sender.Send(command, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    [HttpPatch("items/{id:guid}/qc")]
    public async Task<IActionResult> UpdateQcStatus(
        Guid id,
        [FromBody] UpdateWarehouseItemQcRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateQcStatusCommand(id, request.QcStatus, request.Notes);
        return (await _sender.Send(command, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    [HttpGet("items")]
    public async Task<IActionResult> GetItems(
        [FromQuery] WarehouseQcStatus? qcStatus = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetWarehouseItemsQuery(qcStatus, page, pageSize);
        return (await _sender.Send(query, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    [HttpGet("items/unbatched")]
    public async Task<IActionResult> GetUnbatchedItems(CancellationToken cancellationToken)
    {
        var query = new GetUnbatchedItemsQuery();
        return (await _sender.Send(query, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    [HttpGet("drivers/active-locations")]
    public async Task<IActionResult> GetActiveDriverLocations(CancellationToken cancellationToken)
    {
        var query = new GetActiveDriverLocationsQuery();
        return (await _sender.Send(query, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    [HttpGet("delivery-batches/{batchId:guid}")]
    public async Task<IActionResult> GetDeliveryBatchDetails(Guid batchId, CancellationToken cancellationToken)
    {
        var query = new GetBatchDetailsQuery(batchId);
        return (await _sender.Send(query, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    [HttpPost("delivery-batches")]
    public async Task<IActionResult> CreateDeliveryBatch(
        [FromBody] CreateDeliveryBatchRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateDeliveryBatchCommand(request.OrderIds, request.DriverId);
        return (await _sender.Send(command, cancellationToken).ConfigureAwait(false))
            .ToCreatedActionResult(HttpContext);
    }

    public sealed record RecordItemArrivalRequest(Guid OrderId, string? Notes);

    public sealed record UpdateWarehouseItemQcRequest(WarehouseQcStatus QcStatus, string? Notes);

    public sealed record CreateDeliveryBatchRequest(IReadOnlyList<Guid> OrderIds, Guid DriverId);
}
