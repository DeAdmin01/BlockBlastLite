using BlockBlastLite.Models;

namespace BlockBlastLite.Services;

public interface IDynamicThemeService
{
    bool IsSupported { get; }
    Task<DynamicPalette?> GetDynamicPaletteAsync();
}
