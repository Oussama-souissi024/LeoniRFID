using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeoniRFID.Helpers;
using LeoniRFID.Models;
using LeoniRFID.Services;
using System.Collections.ObjectModel;

namespace LeoniRFID.ViewModels;

// 🎓 ViewModel pour la page "Liste des Machines"
// Affiche toutes les machines avec filtrage par statut et département
public partial class MachineListViewModel : BaseViewModel
{
    private readonly SupabaseService _supabase;

    public MachineListViewModel(SupabaseService supabase)
    {
        _supabase = supabase;
        Title = "Liste des Machines";
    }

    public ObservableCollection<Machine> Machines { get; } = [];

    [ObservableProperty] private string _selectedFilter = "Tous";
    public List<string> FilterOptions { get; } = ["Tous", "Running", "Broken", "InMaintenance", "Paused", "Removed"];

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var all = await _supabase.GetAllMachinesAsync();

            var filtered = SelectedFilter == "Tous"
                ? all
                : all.Where(m => m.Status == SelectedFilter).ToList();

            Machines.Clear();
            foreach (var m in filtered.OrderBy(x => x.Name))
                Machines.Add(m);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ViewDetailAsync(Machine machine)
    {
        if (machine is null) return;
        await Shell.Current.GoToAsync($"machinedetail?machineId={machine.Id}");
    }
}
