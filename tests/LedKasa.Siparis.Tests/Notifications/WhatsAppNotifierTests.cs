using System.Net;
using FluentAssertions;
using LedKasa.Siparis.Features.Notifications;
using LedKasa.Siparis.Features.Orders.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LedKasa.Siparis.Tests.Notifications;

public class WhatsAppNotifierTests
{
    [Fact]
    public async Task Disabled_ShouldNotCallApi()
    {
        var handler = new RecordingHandler();
        var notifier = Create(handler, token: "", phoneNumberId: "", recipients: "");

        await notifier.NotifyOrderCreatedAsync(Sample());

        handler.Calls.Should().Be(0);
    }

    [Fact]
    public async Task Enabled_ShouldPostTextMessage()
    {
        var handler = new RecordingHandler();
        var notifier = Create(handler, token: "secret-token", phoneNumberId: "12345", recipients: "05551112233");

        await notifier.NotifyOrderCreatedAsync(Sample());

        handler.Calls.Should().Be(1);
        handler.Path.Should().Be("/v21.0/12345/messages");
        handler.Authorization.Should().Be("Bearer secret-token");
        handler.Body.Should().Contain("\"messaging_product\":\"whatsapp\"");
        handler.Body.Should().Contain("\"to\":\"905551112233\"");
        handler.Body.Should().Contain("\"type\":\"text\"");
        handler.Body.Should().Contain("Yeni sipariş");
        handler.Body.Should().Contain("LK-1");
    }

    [Fact]
    public async Task Template_ShouldPostTemplateMessage()
    {
        var handler = new RecordingHandler();
        var notifier = Create(
            handler,
            token: "secret-token",
            phoneNumberId: "12345",
            recipients: "905551112233",
            templateName: "yeni_siparis");

        await notifier.NotifyOrderCreatedAsync(Sample());

        handler.Calls.Should().Be(1);
        handler.Body.Should().Contain("\"type\":\"template\"");
        handler.Body.Should().Contain("\"name\":\"yeni_siparis\"");
        handler.Body.Should().Contain("\"code\":\"tr\"");
        handler.Body.Should().Contain("LK-1");
        handler.Body.Should().Contain("Ali");
    }

    [Fact]
    public async Task MultipleRecipients_ShouldSendEach()
    {
        var handler = new RecordingHandler();
        var notifier = Create(handler, token: "t", phoneNumberId: "1", recipients: "5551112233,5321112233");

        await notifier.NotifyOrderCreatedAsync(Sample());

        handler.Calls.Should().Be(2);
        handler.Bodies.Should().Contain(b => b.Contains("\"to\":\"905551112233\""));
        handler.Bodies.Should().Contain(b => b.Contains("\"to\":\"905321112233\""));
    }

    [Fact]
    public async Task ApiError_ShouldNotThrow()
    {
        var handler = new RecordingHandler { Status = HttpStatusCode.BadRequest };
        var notifier = Create(handler, token: "t", phoneNumberId: "1", recipients: "905551112233");

        var act = async () => await notifier.NotifyOrderCreatedAsync(Sample());
        await act.Should().NotThrowAsync();
    }

    private static WhatsAppNotifier Create(
        RecordingHandler handler,
        string token,
        string phoneNumberId,
        string recipients,
        string? templateName = null)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://graph.facebook.com/") };
        var options = Options.Create(new WhatsAppOptions
        {
            AccessToken = token,
            PhoneNumberId = phoneNumberId,
            Recipients = recipients,
            TemplateName = templateName
        });
        return new WhatsAppNotifier(http, options, NullLogger<WhatsAppNotifier>.Instance);
    }

    private static WhatsAppOrderNotice Sample() =>
        new(1, "LK-1", "Ali", new DateOnly(2026, 9, 12), new DateOnly(2026, 9, 13),
            DeliveryPlace.Sirket, null, "Admin",
            [new WhatsAppOrderLine("CNC LED Kasa", 10, 10, 5, 1, PanelSide.TekYon, null)]);

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public string? Path { get; private set; }
        public string? Authorization { get; private set; }
        public string? Body => Bodies.Count == 0 ? null : Bodies[^1];
        public List<string> Bodies { get; } = [];
        public HttpStatusCode Status { get; init; } = HttpStatusCode.OK;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            Path = request.RequestUri?.PathAndQuery;
            Authorization = request.Headers.Authorization?.ToString();
            Bodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(Status)
            {
                Content = new StringContent("{\"error\":{\"message\":\"fail\"}}")
            };
        }
    }
}
