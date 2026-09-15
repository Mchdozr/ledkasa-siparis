using FluentAssertions;
using LedKasa.Siparis.Features.Catalog;

namespace LedKasa.Siparis.Tests.Catalog;

public class ProductDefaultsTests
{
    [Fact]
    public void ResolveCncProductId_ShouldPreferExactCncName()
    {
        var products = new[]
        {
            new ProductDto { Id = 2, Name = "Kapaksız LED Kabinet", SortOrder = 1 },
            new ProductDto { Id = 1, Name = "CNC LED Kasa", SortOrder = 2 }
        };

        ProductDefaults.ResolveCncProductId(products).Should().Be(1);
    }

    [Fact]
    public void ResolveCncProductId_ShouldMatchNameContainingCnc()
    {
        var products = new[]
        {
            new ProductDto { Id = 7, Name = "Kapaksız LED Kabinet" },
            new ProductDto { Id = 9, Name = "CNC Kasa" }
        };

        ProductDefaults.ResolveCncProductId(products).Should().Be(9);
    }

    [Fact]
    public void ResolveCncProductId_ShouldFallbackToOne_WhenEmpty()
    {
        ProductDefaults.ResolveCncProductId([]).Should().Be(1);
    }
}
