using LeoniRFID.ViewModels;

namespace LeoniRFID.Views;

public partial class LoginPage : ContentPage
{
    // Commentaire pédagogique :
    // - `LoginPage` est la vue XAML associée à `LoginViewModel`.
    // - Le constructeur reçoit le ViewModel via l'injection de dépendances (DI) configurée dans `MauiProgram`.
    // - `BindingContext` relie la vue aux propriétés/commandes du ViewModel.
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
