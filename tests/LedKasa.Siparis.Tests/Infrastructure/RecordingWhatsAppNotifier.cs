using LedKasa.Siparis.Features.Notifications;

namespace LedKasa.Siparis.Tests.Infrastructure;

internal sealed class RecordingWhatsAppNotifier : IWhatsAppNotifier
{
    public WhatsAppOrderNotice? Last { get; private set; }
    public int CallCount { get; private set; }

    public Task NotifyOrderCreatedAsync(WhatsAppOrderNotice order, CancellationToken cancellationToken = default)
    {
        Last = order;
        CallCount++;
        return Task.CompletedTask;
    }
}
