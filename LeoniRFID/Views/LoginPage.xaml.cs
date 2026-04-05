using LeoniRFID.ViewModels;

namespace LeoniRFID.Views;

public partial class LoginPage : ContentPage
{
    // 🎓 Pédagogie PFE : Initialisation de l'Authentication
    // La page de connexion est la route par défaut.
    // L'injection de dépendances `(LoginViewModel viewModel)` fournit
    // l'instance nécessaire pour que la vue puisse exécuter les commandes (LoginCommand).
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
