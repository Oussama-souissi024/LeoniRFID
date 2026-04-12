using LeoniRFID.ViewModels;

namespace LeoniRFID.Views;

public partial class MachineListPage : ContentPage
{
    private readonly MachineListViewModel _vm;

    public MachineListPage(MachineListViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }
}
