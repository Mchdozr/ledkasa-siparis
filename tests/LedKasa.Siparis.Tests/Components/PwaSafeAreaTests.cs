using FluentAssertions;

namespace LedKasa.Siparis.Tests.Components;

public class PwaSafeAreaTests
{
    [Fact]
    public void AppCss_ShouldPadStandaloneStatusBar()
    {
        var css = File.ReadAllText(FindAppCss());
        css.Should().Contain("safe-area-inset-top");
        css.Should().Contain("display-mode: standalone");
        css.Should().Contain("html.lk-standalone");
        css.Should().Contain("padding-top: var(--lk-safe-top)");
    }

    private static string FindAppCss()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var path = Path.Combine(dir.FullName, "src", "LedKasa.Siparis", "wwwroot", "app.css");
            if (File.Exists(path))
                return path;

            dir = dir.Parent;
        }

        throw new FileNotFoundException("app.css");
    }
}
