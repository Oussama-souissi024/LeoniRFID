namespace LeoniRFID;

public partial class App : Application
{
    // 🎓 Pédagogie PFE : Le point de départ (Classe Globale App)
    // C'est le tout premier code exécuté après le démarrage du processus MAUI.
    // L'appel à `InitializeComponent()` charge `App.xaml` (qui consolide en mémoire 
    // tous les dictionnaires de couleurs, de styles et les converters).
    private readonly IServiceProvider _serviceProvider;

    public App(IServiceProvider serviceProvider)
    {
        // Charger les ressources XAML (styles, converters, etc.). Si une ressource manque, InitializeComponent peut lever une exception.
        InitializeComponent();
        _serviceProvider = serviceProvider;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Retourner la fenêtre principale qui contient l'AppShell (navigation par Shell).
        return new Window(new AppShell(_serviceProvider));
    }
}
