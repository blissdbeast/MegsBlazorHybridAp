using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.LifecycleEvents;

#if ANDROID
using AndroidX.Core.View;
#endif

#if IOS || MACCATALYST
using UIKit;
using Microsoft.Maui.Platform; // .ToPlatform()
#endif

namespace GabsHybridApp.Maui.Services;

public static class StatusBarThemeExtensions
{
    /// <summary>
    /// Applies theme colors to the system status bar dynamically at runtime.
    /// In dark mode, sets dark background (#0f172a) with light text/icons.
    /// In light mode, sets primary blue background (#2979ff) with light text/icons.
    /// </summary>
    public static void ApplyTheme(bool isDarkMode)
    {
        var hex = isDarkMode ? "#0f172a" : "#2979ff";
        var lightContent = true;
        SetStatusBar(hex, lightContent);
    }

    /// <summary>
    /// Updates the status bar color and icon brightness dynamically at runtime.
    /// </summary>
    public static void SetStatusBar(string hex, bool lightContent)
    {
#if ANDROID
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                var window = Platform.CurrentActivity?.Window;
                if (window is null) return;

                var color = global::Android.Graphics.Color.ParseColor(NormalizeHex(hex));

#pragma warning disable CA1422
                window.SetStatusBarColor(color);
#pragma warning restore CA1422

                var ctl = new WindowInsetsControllerCompat(window, window.DecorView);
                ctl.AppearanceLightStatusBars = !lightContent;       // false => light icons, true => dark icons
                ctl.AppearanceLightNavigationBars = !lightContent;
            }
            catch
            {
                // Graceful fallback if window or activity is unavailable
            }
        });
#elif IOS || MACCATALYST
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                ApplyAppleAppearance(hex, lightContent);
            }
            catch
            {
                // Graceful fallback
            }
        });
#endif
    }

    /// <summary>
    /// Sets the system status bar color app-wide.
    /// <paramref name="hex"/> accepts "#RRGGBB" or "#AARRGGBB" (leading '#' optional).
    /// <paramref name="lightContent"/> = true for light (white) status bar text/icons; false for dark.
    /// </summary>
    public static MauiAppBuilder UseStatusBarTheme(this MauiAppBuilder builder, string hex, bool lightContent)
    {
        builder.ConfigureLifecycleEvents(events =>
        {
#if ANDROID
            events.AddAndroid(android => android.OnCreate((activity, _) =>
            {
                var window = activity?.Window;
                if (window is null) return;

                // Fully-qualify to avoid conflict with Microsoft.Maui.Graphics.Color
                var color = global::Android.Graphics.Color.ParseColor(NormalizeHex(hex));

#pragma warning disable CA1422
                window.SetStatusBarColor(color);
#pragma warning restore CA1422

                var ctl = new WindowInsetsControllerCompat(window, window.DecorView);
                ctl.AppearanceLightStatusBars = !lightContent;       // false => light icons, true => dark icons
                ctl.AppearanceLightNavigationBars = !lightContent;
            }));
#endif

#if IOS || MACCATALYST
            // Use AddiOS for BOTH iOS and MacCatalyst to avoid AddMacCatalyst editor errors
            events.AddiOS(ios => ios.FinishedLaunching((app, _) =>
            {
                ApplyAppleAppearance(hex, lightContent);
                return true;
            }));
#endif
        });

        return builder;
    }

#if ANDROID
    private static string NormalizeHex(string h) =>
        string.IsNullOrWhiteSpace(h) ? "#000000" : (h[0] == '#' ? h : $"#{h}");
#endif

#if IOS || MACCATALYST
    private static void ApplyAppleAppearance(string hex, bool lightContent)
    {
        var barColor = FromHex(hex);

        var nav = new UINavigationBarAppearance();
        nav.ConfigureWithOpaqueBackground();
        nav.BackgroundColor = barColor;

        var titleColor = lightContent ? UIColor.White : UIColor.Black;
        nav.TitleTextAttributes = new UIStringAttributes { ForegroundColor = titleColor };
        nav.LargeTitleTextAttributes = new UIStringAttributes { ForegroundColor = titleColor };

        UINavigationBar.Appearance.StandardAppearance   = nav;
        UINavigationBar.Appearance.ScrollEdgeAppearance = nav;
        UINavigationBar.Appearance.CompactAppearance    = nav;

        var tool = new UIToolbarAppearance();
        tool.ConfigureWithOpaqueBackground();
        tool.BackgroundColor = barColor;
        UIToolbar.Appearance.StandardAppearance   = tool;
        UIToolbar.Appearance.ScrollEdgeAppearance = tool;

#pragma warning disable CA1422
        UIApplication.SharedApplication.StatusBarStyle =
            lightContent ? UIStatusBarStyle.LightContent : UIStatusBarStyle.DarkContent;
#pragma warning restore CA1422
    }

    // Parse hex with MAUI's color and convert to UIColor (handles #RGB, #RRGGBB, #AARRGGBB)
    private static UIColor FromHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) hex = "#000000";
        if (!hex.StartsWith("#")) hex = "#" + hex;

        var gcolor = global::Microsoft.Maui.Graphics.Color.FromArgb(hex);
        return gcolor.ToPlatform();
    }
#endif
}
