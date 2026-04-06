using LeoniRFID.ViewModels;

namespace LeoniRFID.Views;

// 🎓 Pédagogie PFE : Le Code-Behind (fichier .xaml.cs)
// En architecture MVVM (Model-View-ViewModel), la Vue est la couche de présentation pure.
// Le ViewModel est passé ici par "Injection de Dépendances". MAUI s'occupe de l'instancier.
// Toute l'intelligence (les appels à Supabase, les calculs de stats) se trouve dans le ViewModel.
// `BindingContext = _viewModel` est la ligne magique qui connecte le XAML au C#.
public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _viewModel;

    public DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
