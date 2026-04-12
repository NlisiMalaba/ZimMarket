using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZimMarket.Application;
using ZimMarket.Application.Common.Abstractions;
using ZimMarket.Application.Common.Models;

namespace ZimMarket.Application.Tests;

public sealed class MediatRPipelineTests
{
    [Fact]
    public async Task Pipeline_delivers_request_to_handler_and_returns_result()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddApplication(typeof(MediatRPipelineTests).Assembly);

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var request = new PipelineTestQuery("ping");
        var result = await sender.Send(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("ping:ok");
    }

    /// <summary>
    /// Read-side query so transaction behaviour no-ops; no validators; not cacheable — exercises the full pipeline stack.
    /// </summary>
    public sealed record PipelineTestQuery(string Payload) : IQuery<string>;

    public sealed class PipelineTestHandler : IRequestHandler<PipelineTestQuery, Result<string>>
    {
        public Task<Result<string>> Handle(PipelineTestQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Result<string>.Success($"{request.Payload}:ok"));
        }
    }
}
