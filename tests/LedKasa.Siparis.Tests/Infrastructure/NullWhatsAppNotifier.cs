using LedKasa.Siparis.Features.Notifications;

namespace LedKasa.Siparis.Tests.Infrastructure;

internal sealed class NullWhatsAppNotifier : IWhatsAppNotifier
{
    public Task NotifyOrderCreatedAsync(WhatsAppOrderNotice order, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
