using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using System.Linq;

namespace BlockBlastLite;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // 1. Hardware acceleration flags on native window
        Window?.SetFlags(WindowManagerFlags.HardwareAccelerated, WindowManagerFlags.HardwareAccelerated);

        // 2. Lock high refresh rate (120Hz / 144Hz)
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            Window?.SetPreferMinimalPostProcessing(true);
            var display = Display;
            if (display != null)
            {
                var modes = display.GetSupportedModes();
                var maxMode = modes?.OrderByDescending(m => m.RefreshRate).FirstOrDefault();
                if (maxMode != null && Window?.Attributes != null)
                {
                    Window.Attributes.PreferredDisplayModeId = maxMode.ModeId;
                }
            }
        }
        else if (OperatingSystem.IsAndroidVersionAtLeast(23))
        {
            if (Window?.Attributes != null)
            {
                Window.Attributes.PreferredRefreshRate = 120.0f;
            }
        }
    }

    protected override void OnPause()
    {
        base.OnPause();
        PauseWebViewMedia();
    }

    protected override void OnStop()
    {
        base.OnStop();
        PauseWebViewMedia();
    }

    protected override void OnResume()
    {
        base.OnResume();
        ResumeWebViewMedia();
    }

    private void PauseWebViewMedia()
    {
        try
        {
            FindAndActOnWebViews(Window?.DecorView, wv => wv.OnPause());
        }
        catch { }
    }

    private void ResumeWebViewMedia()
    {
        try
        {
            FindAndActOnWebViews(Window?.DecorView, wv => wv.OnResume());
        }
        catch { }
    }

    private static void FindAndActOnWebViews(Android.Views.View? view, System.Action<Android.Webkit.WebView> action)
    {
        if (view is Android.Webkit.WebView wv)
        {
            action(wv);
        }
        else if (view is ViewGroup vg)
        {
            for (int i = 0; i < vg.ChildCount; i++)
            {
                FindAndActOnWebViews(vg.GetChildAt(i), action);
            }
        }
    }
}

