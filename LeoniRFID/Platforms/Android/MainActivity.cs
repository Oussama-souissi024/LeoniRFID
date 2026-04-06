// 🎓 Pédagogie PFE : Le Point d'Entrée Android (MainActivity)
// - `MainActivity` est la passerelle native entre le code de notre application et le cœur d'Android.
// - L'attribut [Activity] permet de "configurer" Android sans utiliser de XML externe (manifeste),
//   par exemple pour bloquer l'orientation de l'écran en Portrait (ScreenOrientation.Portrait).
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
        // Exemple : forcer un thème à l'initialisation si nécessaire.
        // Microsoft.Maui.ApplicationModel.AppInfo.Current.RequestedTheme = Microsoft.Maui.ApplicationModel.AppTheme.Dark;
    }
}
