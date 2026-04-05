// 🎓 Pédagogie PFE : La Configuration de l'Application Native Android
// - `MainApplication` indique au système d'exploitation Android comment lancer notre code .NET MAUI.
// - Android lance d'abord ce fichier, qui appelle ensuite `MauiProgram.CreateMauiApp()`
//   pour initialiser nos pages, nos services et nos ViewModels.
using Android.App;
using Android.Runtime;

namespace LeoniRFID;

[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
