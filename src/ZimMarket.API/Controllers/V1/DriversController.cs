using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZimMarket.API.Http;
using ZimMarket.Application.Auth;
using ZimMarket.Application.Drivers;
using ZimMarket.Application.Logistics;

namespace ZimMarket.API.Controllers.V1;

[ApiController]
[Route("api/v1/drivers")]
[Authorize(Policy = AuthorizationPolicies.Driver)]
public sealed class DriversController : ControllerBase
{
    private readonly ISender _sender;

    public DriversController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    [HttpGet("batches/{batchId:guid}")]
    public async Task<IActionResult> GetBatchDetails(Guid batchId, CancellationToken cancellationToken)
    {
        var query = new GetBatchDetailsQuery(batchId);
        return (await _sender.Send(query, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    [HttpPost("location")]
    public async Task<IActionResult> UpdateLocation(
        [FromBody] UpdateDriverLocationRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateDriverLocationCommand(request.Latitude, request.Longitude);
        return (await _sender.Send(command, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    [HttpPost("batches/{batchId:guid}/confirm-collected")]
    public async Task<IActionResult> ConfirmBatchCollected(
        Guid batchId,
        CancellationToken cancellationToken)
    {
        var command = new ConfirmBatchCollectedCommand(batchId);
        return (await _sender.Send(command, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    [HttpPost("batches/{batchId:guid}/orders/{orderId:guid}/confirm-delivery")]
    public async Task<IActionResult> ConfirmDelivery(
        Guid batchId,
        Guid orderId,
        [FromBody] ConfirmDeliveryRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ConfirmDeliveryCommand(batchId, orderId, request.DeliveryPhotoKey);
        return (await _sender.Send(command, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    public sealed record UpdateDriverLocationRequest(double Latitude, double Longitude);

    public sealed record ConfirmDeliveryRequest(string DeliveryPhotoKey);
}
