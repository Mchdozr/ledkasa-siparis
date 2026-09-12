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
}
