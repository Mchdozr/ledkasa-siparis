using Microsoft.AspNetCore.Components.Server.Circuits;

namespace LedKasa.Siparis.Infrastructure;

internal sealed class CircuitLifecycleLogger(ILogger<CircuitLifecycleLogger> logger) : CircuitHandler
{
    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        logger.LogInformation("Blazor circuit opened {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        logger.LogInformation("Blazor circuit closed {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        logger.LogWarning("Blazor circuit connection down {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }
}
