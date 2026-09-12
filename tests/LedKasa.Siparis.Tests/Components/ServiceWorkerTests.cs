using FluentAssertions;

namespace LedKasa.Siparis.Tests.Components;

public class ServiceWorkerTests
{
    [Fact]
    public void ShouldBypassBlazorCircuitAndSkipClaim()
    {
        var js = File.ReadAllText(Find("src/LedKasa.Siparis/wwwroot/service-worker.js"));
        js.Should().Contain("/_blazor");
        js.Should().Contain("/_framework");
        js.Should().NotContain("clients.claim");
    }

    private static string Find(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var path = Path.Combine(dir.FullName, relative);
            if (File.Exists(path))
                return path;

            dir = dir.Parent;
        }

        throw new FileNotFoundException(relative);
    }
}
