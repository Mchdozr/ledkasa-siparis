using FluentAssertions;
using LedKasa.Siparis.Features.Notifications;

namespace LedKasa.Siparis.Tests.Notifications;

public class TelegramCommandsTests
{
    [Theory]
    [InlineData("/start", true)]
    [InlineData("/START", true)]
    [InlineData("/start@Ledkasasiparisbot", true)]
    [InlineData("/start hello", true)]
    [InlineData("Start", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsStart_ShouldMatchBotCommand(string? text, bool expected)
        => TelegramCommands.IsStart(text).Should().Be(expected);

    [Theory]
    [InlineData("/id", true)]
    [InlineData("/ID", true)]
    [InlineData("/id@Ledkasasiparisbot", true)]
    [InlineData("id", false)]
    public void IsId_ShouldMatchBotCommand(string? text, bool expected)
        => TelegramCommands.IsId(text).Should().Be(expected);

    [Fact]
    public void ReadyReply_ShouldIncludeChatId()
        => TelegramCommands.ReadyReply("-1001234567890").Should().Contain("-1001234567890");
}
