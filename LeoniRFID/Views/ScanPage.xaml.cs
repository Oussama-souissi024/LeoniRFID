using LeoniRFID.ViewModels;

namespace LeoniRFID.Views;

// 🎓 Pédagogie PFE : Écouteur d'Événements du Cycle de Vie
// Les applications mobiles tournent sur batterie. Il est impératif de couper
// les composants matériels (Bluetooth, RFID Zebra, GPS) quand l'utilisateur ne regarde plus la vue.
// `OnDisappearing()` est déclenché par le système d'exploitation quand on change d'onglet.
public partial class ScanPage : ContentPage
{
    private readonly ScanViewModel _viewModel;

    public ScanPage(ScanViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
    }

    // 🎓 Très important : on stoppe le lecteur RFID quand on quitte la page
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.OnDisappearing();
    }
}
