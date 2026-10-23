using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.AppCompat.App;

namespace MediRappel;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        // Thème clair imposé : la charte ne prévoit pas de variante sombre et
        // les fonds sont clairs en dur (sinon texte système clair illisible).
        AppCompatDelegate.DefaultNightMode = AppCompatDelegate.ModeNightNo;

        base.OnCreate(savedInstanceState);

        Window?.SetStatusBarColor(Android.Graphics.Color.ParseColor("#00696D"));
        Window?.SetNavigationBarColor(Android.Graphics.Color.ParseColor("#00696D"));
        Window?.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
    }
}
