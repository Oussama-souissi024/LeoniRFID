using LeoniRFID.Models;
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

    // 🎓 Pédagogie PFE : Navigation via SelectionChanged
    // Le TapGestureRecognizer ne fonctionne pas de manière fiable avec les
    // DataTemplates compilés en MAUI. On utilise donc l'événement natif
    // SelectionChanged du CollectionView, qui fonctionne à 100%.
    private async void OnMachineSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Machine machine)
            return;

        // Réinitialiser la sélection pour permettre de re-cliquer
        if (sender is CollectionView cv)
            cv.SelectedItem = null;

        // Naviguer vers la page de session de maintenance
        await Shell.Current.GoToAsync($"maintenancesession?machineId={machine.Id}");
    }
}
