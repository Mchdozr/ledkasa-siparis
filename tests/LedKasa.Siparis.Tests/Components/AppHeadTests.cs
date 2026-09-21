using FluentAssertions;

namespace LedKasa.Siparis.Tests.Components;

public class AppHeadTests
{
    [Fact]
    public void ShouldNotReferenceMissingScopedCssBundle()
    {
        var html = File.ReadAllText(FindAppRazor());
        html.Should().NotContain("LedKasa.Siparis.styles.css");
        html.Should().Contain("href=\"app.css\"");
    }

    [Fact]
    public void ShouldDisableInteractiveServerPrerender()
    {
        var html = File.ReadAllText(FindAppRazor());
        html.Should().Contain("prerender: false");
        html.Should().NotContain("@rendermode=\"InteractiveServer\"");
    }

    [Fact]
    public void ShouldDeclareModernAndAppleWebAppCapable()
    {
        var html = File.ReadAllText(FindAppRazor());
        html.Should().Contain("name=\"mobile-web-app-capable\"");
        html.Should().Contain("name=\"apple-mobile-web-app-capable\"");
    }

    private static string FindAppRazor()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var path = Path.Combine(dir.FullName, "src", "LedKasa.Siparis", "Components", "App.razor");
            if (File.Exists(path))
                return path;

            dir = dir.Parent;
        }

        throw new FileNotFoundException("App.razor");
    }
}
