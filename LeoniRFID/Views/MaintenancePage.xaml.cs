using LeoniRFID.ViewModels;

namespace LeoniRFID.Views;

public partial class MaintenancePage : ContentPage
{
    private readonly MaintenanceViewModel _vm;

    public MaintenancePage(MaintenanceViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.OnDisappearing();
    }
}
