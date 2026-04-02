using LeoniRFID.ViewModels;

namespace LeoniRFID.Views;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _viewModel;

    public DashboardPage(DashboardViewModel viewModel)
    {
        // Commentaire pédagogique :
        // - Le Dashboard charge dynamiquement les données à l'apparition (`OnAppearing`) via le ViewModel.
        // - Éviter de faire de lourdes opérations dans le constructeur de la page ; préférez `OnAppearing`.
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
