using LeoniRFID.ViewModels;

namespace LeoniRFID.Views;

/// <summary>
/// 🎓 Pédagogie PFE : Le Rôle de la Vue (View)
/// Le code-behind (fichier .cs lié au fichier .xaml) doit rester "stupide" (dumb view).
/// Sa seule mission est de lier l'interface graphique (AdminPage) au cerveau (AdminViewModel).
/// Cette séparation permet de tester le cerveau sans avoir besoin d'afficher l'interface.
/// </summary>
public partial class AdminPage : ContentPage
{
    private readonly AdminViewModel _viewModel;

    public AdminPage(AdminViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Charger les données (machines, utilisateurs) quand la page devient visible
        await _viewModel.LoadAsync();
    }
}
