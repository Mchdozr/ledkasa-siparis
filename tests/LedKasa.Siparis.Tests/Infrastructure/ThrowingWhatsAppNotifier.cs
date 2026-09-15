using LedKasa.Siparis.Features.Notifications;

namespace LedKasa.Siparis.Tests.Infrastructure;

internal sealed class ThrowingWhatsAppNotifier : IWhatsAppNotifier
{
    public Task NotifyOrderCreatedAsync(WhatsAppOrderNotice order, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("whatsapp down");
}
