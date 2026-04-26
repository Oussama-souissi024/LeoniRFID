using LeoniRFID.Models;
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

    private async void OnMachineSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Machine machine)
            return;

        if (sender is CollectionView cv)
            cv.SelectedItem = null;

        await Shell.Current.GoToAsync($"machinedetail?machineId={machine.Id}");
    }
}
