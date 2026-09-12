using LedKasa.Siparis.Features.Notifications;

namespace LedKasa.Siparis.Tests.Infrastructure;

internal sealed class NullTelegramNotifier : ITelegramNotifier
{
    public Task NotifyOrderCreatedAsync(TelegramOrderNotice order, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
