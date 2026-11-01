#if ANDROID
using Android.App;
using Android.Content.Res;
using Android.OS;
#endif
using BlockBlastLite.Models;

namespace BlockBlastLite.Services;

public class DynamicThemeService : IDynamicThemeService
{
    public bool IsSupported
    {
        get
        {
#if ANDROID
            return OperatingSystem.IsAndroidVersionAtLeast(31);
#else
            return false;
#endif
        }
    }

    public Task<DynamicPalette?> GetDynamicPaletteAsync()
    {
#if ANDROID
        try
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                return Task.FromResult<DynamicPalette?>(GetAndroidDynamicPalette());
            }
        }
        catch
        {
            // Graceful fallback
        }
#endif
        return Task.FromResult<DynamicPalette?>(null);
    }

#if ANDROID
    [System.Runtime.Versioning.SupportedOSPlatform("android31.0")]
    private static DynamicPalette? GetAndroidDynamicPalette()
    {
        var context = Android.App.Application.Context;
        if (context?.Resources?.Configuration == null) return null;

        bool isDark = (context.Resources.Configuration.UiMode & UiMode.NightMask) == UiMode.NightYes;

        // Retrieve Material You / Monet system palette from Android 12+ (API 31+)
        int accentPrimary = context.GetColor(isDark ? Android.Resource.Color.SystemAccent1200 : Android.Resource.Color.SystemAccent1600);
        int accentContainer = context.GetColor(isDark ? Android.Resource.Color.SystemAccent1700 : Android.Resource.Color.SystemAccent1100);
        int accentSubtle = context.GetColor(isDark ? Android.Resource.Color.SystemAccent1800 : Android.Resource.Color.SystemAccent150);
        int background = context.GetColor(isDark ? Android.Resource.Color.SystemNeutral1900 : Android.Resource.Color.SystemNeutral150);
        int surface = context.GetColor(isDark ? Android.Resource.Color.SystemNeutral1800 : Android.Resource.Color.SystemNeutral1100);
        int surfaceVariant = context.GetColor(isDark ? Android.Resource.Color.SystemNeutral1700 : Android.Resource.Color.SystemNeutral1200);

        return new DynamicPalette
        {
            AccentPrimary = ToHex(accentPrimary),
            AccentContainer = ToHex(accentContainer),
            AccentSubtle = ToHex(accentSubtle),
            Background = ToHex(background),
            Surface = ToHex(surface),
            SurfaceVariant = ToHex(surfaceVariant),
            TextPrimary = isDark ? "#F8FAFC" : "#0F172A",
            IsDark = isDark
        };
    }

    private static string ToHex(int colorInt)
    {
        return $"#{colorInt & 0x00FFFFFF:X6}";
    }
#endif
}
