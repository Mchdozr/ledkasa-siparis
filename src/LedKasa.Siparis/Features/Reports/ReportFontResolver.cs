using PdfSharpCore.Fonts;

namespace LedKasa.Siparis.Features.Reports;

internal sealed class ReportFontResolver : IFontResolver
{
    internal const string Family = "DejaVu Sans";
    private const string RegularFace = "DejaVuSans";
    private const string BoldFace = "DejaVuSans-Bold";

    internal static readonly ReportFontResolver Instance = new();

    private readonly byte[] _regular = Load("DejaVuSans.ttf");
    private readonly byte[] _bold = Load("DejaVuSans-Bold.ttf");

    public string DefaultFontName => Family;

    public byte[] GetFont(string faceName) =>
        faceName.Contains("Bold", StringComparison.OrdinalIgnoreCase) ? _bold : _regular;

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic) =>
        new(isBold ? BoldFace : RegularFace, isBold, false);

    private static byte[] Load(string fileName)
    {
        var resource = $"LedKasa.Siparis.Fonts.{fileName}";
        using var stream = typeof(ReportFontResolver).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Gömülü font bulunamadı: {resource}");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
