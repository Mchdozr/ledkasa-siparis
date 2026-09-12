using LedKasa.Siparis.Features.Notifications;

namespace LedKasa.Siparis.Tests.Infrastructure;

internal sealed class RecordingTelegramNotifier : ITelegramNotifier
{
    public TelegramOrderNotice? Last { get; private set; }
    public int CallCount { get; private set; }

    public Task NotifyOrderCreatedAsync(TelegramOrderNotice order, CancellationToken cancellationToken = default)
    {
        Last = order;
        CallCount++;
        return Task.CompletedTask;
    }
}
