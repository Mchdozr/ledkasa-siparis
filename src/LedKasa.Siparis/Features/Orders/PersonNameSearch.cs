using System.Globalization;
using LedKasa.Siparis.Common;

namespace LedKasa.Siparis.Features.Orders;

public static class PersonNameSearch
{
    public static IReadOnlyList<string> Filter(IEnumerable<string?> names, string? term, int take = 12)
    {
        var comparer = StringComparer.Create(TurkeyTime.Culture, ignoreCase: true);
        var unique = names
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n!.Trim())
            .Distinct(comparer);

        var trimmed = term?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
            return [];

        return unique
            .Where(name => Matches(name, trimmed))
            .OrderBy(name => name, comparer)
            .Take(take)
            .ToList();
    }

    public static bool Matches(string name, string term)
    {
        var compare = TurkeyTime.Culture.CompareInfo;
        if (compare.IsPrefix(name, term, CompareOptions.IgnoreCase))
            return true;

        foreach (var word in name.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (compare.IsPrefix(word, term, CompareOptions.IgnoreCase))
                return true;
        }

        return false;
    }
}
