using FluentAssertions;
using LedKasa.Siparis.Features.Notifications;

namespace LedKasa.Siparis.Tests.Notifications;

public class WhatsAppPhoneTests
{
    [Theory]
    [InlineData("+90 555 111 22 33", "905551112233")]
    [InlineData("05551112233", "905551112233")]
    [InlineData("5551112233", "905551112233")]
    [InlineData("00905551112233", "905551112233")]
    [InlineData("905551112233", "905551112233")]
    public void Normalize_ShouldAcceptTurkishMobiles(string input, string expected)
    {
        WhatsAppPhone.Normalize(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("123")]
    public void Normalize_ShouldRejectInvalid(string input)
    {
        WhatsAppPhone.Normalize(input).Should().BeNull();
    }

    [Fact]
    public void ParseRecipients_ShouldSplitAndDeduplicate()
    {
        var phones = WhatsAppPhone.ParseRecipients("05551112233, 905551112233; 5321112233");
        phones.Should().Equal("905551112233", "905321112233");
    }
}
