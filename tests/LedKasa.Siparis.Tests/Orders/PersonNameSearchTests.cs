using FluentAssertions;
using LedKasa.Siparis.Features.Orders;

namespace LedKasa.Siparis.Tests.Orders;

public class PersonNameSearchTests
{
    [Fact]
    public void Filter_ShouldMatchFromFirstLetters_IgnoreTurkishCase()
    {
        var names = new[] { "Ahmet Yılmaz", "İbrahim Kaya", "Mehmet Demir" };

        PersonNameSearch.Filter(names, "ah").Should().Equal("Ahmet Yılmaz");
        PersonNameSearch.Filter(names, "ib").Should().Equal("İbrahim Kaya");
        PersonNameSearch.Filter(names, "MEH").Should().Equal("Mehmet Demir");
        PersonNameSearch.Filter(names, "yıl").Should().Equal("Ahmet Yılmaz");
    }

    [Fact]
    public void Filter_ShouldReturnEmpty_WhenTermBlank()
    {
        PersonNameSearch.Filter(["Ahmet Yılmaz"], "  ").Should().BeEmpty();
        PersonNameSearch.Filter(["Ahmet Yılmaz"], null).Should().BeEmpty();
    }

    [Fact]
    public void Filter_ShouldDeduplicateIgnoringCase()
    {
        PersonNameSearch.Filter(["Ahmet Yılmaz", "ahmet yılmaz", "Ayşe"], "ah")
            .Should().Equal("Ahmet Yılmaz");
    }

    [Fact]
    public void UniqueSorted_ShouldOrderIgnoringTurkishCase()
    {
        PersonNameSearch.UniqueSorted(["Mehmet", "ayşe", "Ayşe", "Ahmet"])
            .Should().Equal("Ahmet", "ayşe", "Mehmet");
    }
}
