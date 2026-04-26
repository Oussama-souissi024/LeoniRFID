using LeoniRFID.ViewModels;

namespace LeoniRFID.Views;

public partial class MaintenanceSessionPage : ContentPage
{
    private readonly MaintenanceSessionViewModel _vm;

    public MaintenanceSessionPage(MaintenanceSessionViewModel vm)
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
