using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeoniRFID.Helpers;
using LeoniRFID.Models;
using LeoniRFID.Services;
using System.Collections.ObjectModel;

namespace LeoniRFID.ViewModels;

// 🎓 ViewModel pour la page "Liste des Machines"
// Affiche toutes les machines avec filtrage par statut, nom et département
public partial class MachineListViewModel : BaseViewModel
{
    private readonly SupabaseService _supabase;
    private List<Machine> _allMachines = [];

    public MachineListViewModel(SupabaseService supabase)
    {
        _supabase = supabase;
        Title = "Machine List";
    }

    public ObservableCollection<Machine> Machines { get; } = [];

    // ── Filtres ──────────────────────────────────────────────────────────

    [ObservableProperty] private string _selectedStatus = "All";
    [ObservableProperty] private string _selectedDepartment = "All";
    [ObservableProperty] private string _searchText = string.Empty;

    // 🎓 Les options de statut correspondent aux valeurs réelles de la colonne equipment_status
    public List<string> StatusOptions { get; } =
    [
        "All", "Active", "Passive", "Defect", "Scrapped",
        "InMaintenance", "TransferDone", "TransferOngoing", "TransferAvailable"
    ];

    public ObservableCollection<string> DepartmentOptions { get; } = ["All"];

    // ══════════════════════════════════════════════════════════════════════
    //  CHARGEMENT
    // ══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            _allMachines = await _supabase.GetAllMachinesAsync();

            // Charger les départements uniques depuis les machines
            var plants = _allMachines
                .Select(m => m.Plant)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct()
                .OrderBy(p => p)
                .ToList();

            DepartmentOptions.Clear();
            DepartmentOptions.Add("All");
            foreach (var p in plants)
                DepartmentOptions.Add(p);

            ApplyFilters();
        }
        finally { IsBusy = false; }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  FILTRAGE
    // ══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private void ApplyFilters()
    {
        var filtered = _allMachines.AsEnumerable();

        // Filtre par statut
        if (SelectedStatus != "All")
            filtered = filtered.Where(m => m.EquipmentStatus == SelectedStatus);

        // Filtre par département (Plant)
        if (SelectedDepartment != "All")
            filtered = filtered.Where(m => m.Plant == SelectedDepartment);

        // Filtre par nom (recherche textuelle)
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim().ToLowerInvariant();
            filtered = filtered.Where(m =>
                m.StandardEquipmentName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                m.TagReference.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                m.SerialNumber.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        Machines.Clear();
        foreach (var m in filtered.OrderBy(x => x.StandardEquipmentName))
            Machines.Add(m);
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SelectedStatus = "All";
        SelectedDepartment = "All";
        SearchText = string.Empty;
        ApplyFilters();
    }
}
