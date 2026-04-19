using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZimMarket.API.Http;
using ZimMarket.Application.Auth;
using ZimMarket.Application.Drivers;
using ZimMarket.Application.Logistics;
using ZimMarket.Domain.Enums;

namespace ZimMarket.API.Controllers.V1;

[ApiController]
[Route("api/v1/batches")]
[Authorize(Policy = AuthorizationPolicies.AdminOrAbove)]
public sealed class BatchesController : ControllerBase
{
    private readonly ISender _sender;

    public BatchesController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    [HttpPost]
    public async Task<IActionResult> CreateBatch(
        [FromBody] CreateDeliveryBatchRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateDeliveryBatchCommand(request.OrderIds, request.DriverId);
        return (await _sender.Send(command, cancellationToken).ConfigureAwait(false))
            .ToCreatedActionResult(HttpContext);
    }

    [HttpGet]
    public async Task<IActionResult> GetBatches(
        [FromQuery] DeliveryBatchStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetBatchesQuery(status, page, pageSize);
        return (await _sender.Send(query, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetBatchDetails(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetBatchDetailsQuery(id);
        return (await _sender.Send(query, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    [HttpGet("drivers/locations")]
    public async Task<IActionResult> GetActiveDriverLocations(CancellationToken cancellationToken)
    {
        var query = new GetActiveDriverLocationsQuery();
        return (await _sender.Send(query, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    public sealed record CreateDeliveryBatchRequest(IReadOnlyList<Guid> OrderIds, Guid DriverId);
}
