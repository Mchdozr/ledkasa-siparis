using MudBlazor;

namespace LedKasa.Siparis.Theming;

public static class LedKasaTheme
{
    public static MudTheme Create() => new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#0B1F3A",
            Secondary = "#F46F2C",
            AppbarBackground = "#0B1F3A",
            AppbarText = "#FFFFFF",
            Background = "#F4F6F8",
            Surface = "#FFFFFF",
            DrawerBackground = "#0B1F3A",
            DrawerText = "#E8EEF5",
            DrawerIcon = "#F46F2C",
            TextPrimary = "#122033",
            TextSecondary = "#5B6B7C",
            ActionDefault = "#0B1F3A",
            Success = "#1F7A4D",
            Warning = "#E3A008",
            Error = "#C62828",
            Info = "#1565C0"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Manrope", "Segoe UI", "sans-serif"]
            },
            H5 = new H5Typography { FontWeight = "700" },
            H6 = new H6Typography { FontWeight = "700" }
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "12px"
        }
    };
}
