namespace LedKasa.Siparis.Features.Catalog;

public static class ProductDefaults
{
    public const string CncLedKasaName = "CNC LED Kasa";

    public static int ResolveCncProductId(IReadOnlyList<ProductDto> products)
    {
        if (products.Count == 0)
            return 1;

        var exact = products.FirstOrDefault(p =>
            string.Equals(p.Name, CncLedKasaName, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
            return exact.Id;

        var cnc = products.FirstOrDefault(p =>
            p.Name.Contains("CNC", StringComparison.OrdinalIgnoreCase));
        return cnc?.Id ?? products[0].Id;
    }
}
