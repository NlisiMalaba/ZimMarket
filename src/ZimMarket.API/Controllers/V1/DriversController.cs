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
[Authorize(Policy = AuthorizationPolicies.DriverActive)]
public sealed class DriversController : ControllerBase
{
    private readonly ISender _sender;

    public DriversController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    [HttpGet("batches/{id:guid}")]
    public async Task<IActionResult> GetBatchDetails(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetBatchDetailsQuery(id);
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

    [HttpPost("batches/{id:guid}/collected")]
    public async Task<IActionResult> ConfirmBatchCollected(Guid id, CancellationToken cancellationToken)
    {
        var command = new ConfirmBatchCollectedCommand(id);
        return (await _sender.Send(command, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    [HttpPost("batches/{id:guid}/orders/{orderId:guid}/delivered")]
    public async Task<IActionResult> ConfirmDelivery(
        Guid id,
        Guid orderId,
        [FromBody] ConfirmDeliveryRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ConfirmDeliveryCommand(id, orderId, request.DeliveryPhotoKey);
        return (await _sender.Send(command, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    public sealed record UpdateDriverLocationRequest(double Latitude, double Longitude);

    public sealed record ConfirmDeliveryRequest(string DeliveryPhotoKey);
}
