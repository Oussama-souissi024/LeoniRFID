namespace LeoniRFID;

public partial class App : Application
{
    // Commentaire pédagogique :
    // - `App` est la classe d'application globale. Son constructeur est appelé dès le démarrage de MAUI.
    // - `InitializeComponent()` charge `App.xaml` et les dictionnaires de ressources (styles, couleurs, converters).
    // - `CreateWindow` est utilisé sur les plateformes desktop pour créer la `Window` principale.
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
