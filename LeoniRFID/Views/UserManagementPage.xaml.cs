using LeoniRFID.ViewModels;

namespace LeoniRFID.Views;

// 🎓 Pédagogie PFE : Lancement Asynchrone dans le Thread UI
// `OnAppearing` est parfait pour lancer des appels réseau (Fetch Data) sans figer le visuel.
// L'appel à `_viewModel.LoadUsersCommand.ExecuteAsync` remplira les listes après que
// l'interface graphique ait été totalement "dessinée", garantissant la fluidité.
public partial class UserManagementPage : ContentPage
{
    private readonly UserManagementViewModel _viewModel;

    public UserManagementPage(UserManagementViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadUsersCommand.ExecuteAsync(null);
    }
}
