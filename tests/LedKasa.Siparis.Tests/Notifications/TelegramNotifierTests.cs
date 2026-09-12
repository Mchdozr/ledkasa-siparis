using System.Net;
using FluentAssertions;
using LedKasa.Siparis.Features.Notifications;
using LedKasa.Siparis.Features.Orders.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LedKasa.Siparis.Tests.Notifications;

public class TelegramNotifierTests
{
    [Fact]
    public async Task Disabled_ShouldNotCallApi()
    {
        var handler = new RecordingHandler();
        var notifier = Create(handler, token: "", chatId: "");

        await notifier.NotifyOrderCreatedAsync(Sample());

        handler.Calls.Should().Be(0);
    }

    [Fact]
    public async Task Enabled_ShouldPostSendMessage()
    {
        var handler = new RecordingHandler();
        var notifier = Create(handler, token: "123:ABC", chatId: "-1001");

        await notifier.NotifyOrderCreatedAsync(Sample());

        handler.Calls.Should().Be(1);
        handler.Path.Should().Be("/bot123:ABC/sendMessage");
        handler.Body.Should().Contain("chat_id=-1001");
        handler.Body.Should().Contain("parse_mode=HTML");
    }

    [Fact]
    public async Task ApiError_ShouldNotThrow()
    {
        var handler = new RecordingHandler { Status = HttpStatusCode.BadRequest };
        var notifier = Create(handler, token: "123:ABC", chatId: "1");

        var act = async () => await notifier.NotifyOrderCreatedAsync(Sample());
        await act.Should().NotThrowAsync();
    }

    private static TelegramNotifier Create(RecordingHandler handler, string token, string chatId)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.telegram.org/") };
        var options = Options.Create(new TelegramOptions { BotToken = token, ChatId = chatId });
        return new TelegramNotifier(http, options, NullLogger<TelegramNotifier>.Instance);
    }

    private static TelegramOrderNotice Sample() =>
        new(1, "LK-1", "Ali", new DateOnly(2026, 9, 12), new DateOnly(2026, 9, 13),
            DeliveryPlace.Sirket, "Adres", null, Currency.Try, 10, "Admin",
            [new TelegramOrderLine("CNC LED Kasa", 10, 10, 1, 10, 10, null, [])]);

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public string? Path { get; private set; }
        public string? Body { get; private set; }
        public HttpStatusCode Status { get; init; } = HttpStatusCode.OK;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            Path = request.RequestUri?.PathAndQuery;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(Status)
            {
                Content = new StringContent("{\"ok\":false}")
            };
        }
    }
}
