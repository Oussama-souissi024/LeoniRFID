using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeoniRFID.Helpers;
using LeoniRFID.Models;
using LeoniRFID.Services;
using System.Collections.ObjectModel;

namespace LeoniRFID.ViewModels;

// 🎓 Pédagogie PFE : Tableau de bord Maintenance
// Cette page est le "hub" de l'agent de maintenance.
// Elle liste les machines en panne et en cours de maintenance.
// Au clic sur une machine, elle navigue vers MaintenanceSessionPage
// pour gérer l'intervention (chronomètre, notes, changement de statut).
public partial class MaintenanceViewModel : BaseViewModel
{
    private readonly SupabaseService _supabase;

    public MaintenanceViewModel(SupabaseService supabase)
    {
        _supabase = supabase;
        Title = "Maintenance";
    }

    // ── Collections ──────────────────────────────────────────────────────
    public ObservableCollection<Machine> BrokenMachines { get; } = [];
    public ObservableCollection<Machine> InMaintenanceMachines { get; } = [];
    public ObservableCollection<MaintenanceSession> RecentSessions { get; } = [];

    // ── Compteurs ────────────────────────────────────────────────────────
    [ObservableProperty] private int _brokenCount;
    [ObservableProperty] private int _inMaintenanceCount;

    // ══════════════════════════════════════════════════════════════════════
    //  CHARGEMENT DES DONNÉES
    // ══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var allMachines = await _supabase.GetAllMachinesAsync();

            var broken = allMachines
                .Where(m => m.EquipmentStatus == Constants.StatusBroken)
                .OrderBy(m => m.StandardEquipmentName).ToList();

            var inMaint = allMachines
                .Where(m => m.EquipmentStatus == Constants.StatusInMaintenance)
                .OrderBy(m => m.StandardEquipmentName).ToList();

            BrokenMachines.Clear();
            foreach (var m in broken) BrokenMachines.Add(m);

            InMaintenanceMachines.Clear();
            foreach (var m in inMaint) InMaintenanceMachines.Add(m);

            BrokenCount = broken.Count;
            InMaintenanceCount = inMaint.Count;

            // Charger l'historique des maintenances récentes (terminées)
            var history = await _supabase.GetMaintenanceHistoryAsync();
            RecentSessions.Clear();
            foreach (var s in history.Where(s => !s.IsActive).Take(15))
                RecentSessions.Add(s);
        }
        finally { IsBusy = false; }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  NAVIGATION : Ouvrir la page de session pour une machine
    // ══════════════════════════════════════════════════════════════════════

    // 🎓 Pédagogie PFE : Navigation par route avec paramètre
    // On utilise Shell.Current.GoToAsync avec un paramètre "machineId"
    // qui sera récupéré par le QueryProperty du MaintenanceSessionViewModel.
    [RelayCommand]
    private async Task SelectMachineAsync(Machine machine)
    {
        if (machine is null) return;
        await Shell.Current.GoToAsync($"maintenancesession?machineId={machine.Id}");
    }
}
