using Android.App;
using Android.Content.PM;
using Android.OS;

namespace LeoniRFID;

[Activity(Theme = "@style/Maui.MainTheme.NoActionBar", MainLauncher = true, ScreenOrientation = ScreenOrientation.Portrait, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        
        // Ensure the app remains in Dark Mode as per LEONI requirements
        // Microsoft.Maui.ApplicationModel.AppInfo.Current.RequestedTheme = Microsoft.Maui.ApplicationModel.AppTheme.Dark;
    }
}
