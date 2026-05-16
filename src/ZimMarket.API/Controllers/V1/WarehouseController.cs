using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZimMarket.API.Http;
using ZimMarket.Application.Auth;
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
     [Authorize(Policy = AuthorizationPolicies.Admin)]
    public async Task<IActionResult> RecordArrival(
        [FromBody] RecordItemArrivalRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RecordItemArrivalCommand(request.OrderId, request.Notes);
        return (await _sender.Send(command, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    [HttpPatch("items/{id:guid}/qc")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
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
    [Authorize(Policy = AuthorizationPolicies.Admin)]
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
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public async Task<IActionResult> GetUnbatchedItems(CancellationToken cancellationToken)
    {
        var query = new GetUnbatchedItemsQuery();
        return (await _sender.Send(query, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    public sealed record RecordItemArrivalRequest(Guid OrderId, string? Notes);

    public sealed record UpdateWarehouseItemQcRequest(WarehouseQcStatus QcStatus, string? Notes);
}
