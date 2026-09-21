using FluentAssertions;

namespace LedKasa.Siparis.Tests.Infrastructure;

public class CircuitHostWiringTests
{
    [Fact]
    public void Program_UsesPerCallDbContextFactory()
    {
        var src = File.ReadAllText(FindSource("src", "LedKasa.Siparis", "Program.cs"));
        src.Should().Contain("AddDbContextFactory<ApplicationDbContext>");
        src.Should().Contain("IOrdersDbContextFactory");
        src.Should().Contain("ICatalogDbContextFactory");
        src.Should().Contain("IReportingDbContextFactory");
        src.Should().Contain("Circuit:DetailedErrors");
        src.Should().Contain("CircuitLifecycleLogger");
        src.Should().NotContain("AddScoped<IOrdersDbContext>");
    }

    private static string FindSource(params string[] parts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var path = Path.Combine(new[] { dir.FullName }.Concat(parts).ToArray());
            if (File.Exists(path))
                return path;

            dir = dir.Parent;
        }

        throw new FileNotFoundException(string.Join('/', parts));
    }
}
