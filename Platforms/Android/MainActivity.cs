using Android.App;
using Android.Content.PM;
using Android.OS;

namespace WorkJournal
{
    [Activity(ScreenOrientation = ScreenOrientation.Landscape, Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
    }
}
