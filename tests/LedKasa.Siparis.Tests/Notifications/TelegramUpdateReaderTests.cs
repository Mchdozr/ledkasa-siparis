using FluentAssertions;
using LedKasa.Siparis.Features.Notifications;

namespace LedKasa.Siparis.Tests.Notifications;

public class TelegramUpdateReaderTests
{
    [Fact]
    public void Read_ShouldCaptureGroupStartCommand()
    {
        var json = """
            {"ok":true,"result":[{"update_id":10,"message":{"text":"/start","chat":{"id":-1001234567890,"type":"supergroup"}}}]}
            """;

        var signals = TelegramUpdateReader.Read(json);

        signals.Should().ContainSingle().Which.ChatId.Should().Be("-1001234567890");
        TelegramUpdateReader.NextOffset(json, 0).Should().Be(11);
    }

    [Fact]
    public void Read_ShouldCaptureIdCommand()
    {
        var json = """
            {"ok":true,"result":[{"update_id":11,"message":{"text":"/id@Ledkasasiparisbot","chat":{"id":-10055}}}]}
            """;

        TelegramUpdateReader.Read(json).Should().ContainSingle().Which.ChatId.Should().Be("-10055");
    }

    [Fact]
    public void Read_ShouldCaptureBotAddedToGroup()
    {
        var json = """
            {"ok":true,"result":[{"update_id":12,"my_chat_member":{"chat":{"id":-100998877},"old_chat_member":{"status":"left"},"new_chat_member":{"status":"member"}}}]}
            """;

        TelegramUpdateReader.Read(json).Should().ContainSingle().Which.ChatId.Should().Be("-100998877");
    }

    [Fact]
    public void Read_ShouldIgnoreOrdinaryMessages()
    {
        var json = """
            {"ok":true,"result":[{"update_id":13,"message":{"text":"merhaba","chat":{"id":-1001}}}]}
            """;

        TelegramUpdateReader.Read(json).Should().BeEmpty();
        TelegramUpdateReader.NextOffset(json, 0).Should().Be(14);
    }
}
