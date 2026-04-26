using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeoniRFID.Models;
using LeoniRFID.Services;
using System.Collections.ObjectModel;

namespace LeoniRFID.ViewModels;

// 🎓 Pédagogie PFE : Le Tableau de Bord (Dashboard)
// Ce ViewModel est le "cerveau" de la page d'accueil après connexion.
// Il charge les statistiques en temps réel depuis Supabase et gère la navigation.
public partial class DashboardViewModel : BaseViewModel
{
    private readonly SupabaseService _supabase;

    public DashboardViewModel(SupabaseService supabase)
    {
        _supabase = supabase;
        Title = "Dashboard";
    }

    // ── Informations utilisateur connecté ──────────────────────────────
    [ObservableProperty] private string _userName     = string.Empty;
    [ObservableProperty] private string _userRole     = string.Empty;
    [ObservableProperty] private string _userInitials = string.Empty;
    [ObservableProperty] private bool   _isAdmin      = false;
    [ObservableProperty] private bool   _isMaintenance = false;

    // ── Statistiques globales ─────────────────────────────────────────
    [ObservableProperty] private int _totalMachines;
    [ObservableProperty] private int _activeCount;
    [ObservableProperty] private int _defectCount;
    [ObservableProperty] private int _inMaintenanceCount;
    [ObservableProperty] private int _passiveCount;
    [ObservableProperty] private int _scrappedCount;
    [ObservableProperty] private int _transferCount;

    // ── Événements récents ────────────────────────────────────────────
    public ObservableCollection<ScanEvent> RecentEvents { get; } = [];

    // ── Statut de synchronisation ─────────────────────────────────────
    [ObservableProperty] private string _syncStatus = "Cloud connected";
    [ObservableProperty] private string _lastSyncTime = "—";

    // ══════════════════════════════════════════════════════════════════════
    //  CHARGEMENT DES DONNÉES
    // ══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    public async Task LoadDashboard()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            // Utilisateur
            IsAdmin = _supabase.IsAdmin;
            IsMaintenance = _supabase.IsMaintenance || _supabase.IsAdmin;
            UserName = _supabase.CurrentProfile?.FullName ?? "User";
            UserRole = _supabase.CurrentProfile?.RoleDisplay ?? "Unknown";
            UserInitials = _supabase.CurrentProfile?.Initials ?? "?";

            // Statistiques machines
            var machines = await _supabase.GetAllMachinesAsync();
            TotalMachines      = machines.Count;
            ActiveCount        = machines.Count(m => m.EquipmentStatus == "Active");
            DefectCount        = machines.Count(m => m.EquipmentStatus == "Defect");
            InMaintenanceCount = machines.Count(m => m.EquipmentStatus == "InMaintenance");
            PassiveCount       = machines.Count(m => m.EquipmentStatus == "Passive");
            ScrappedCount      = machines.Count(m => m.EquipmentStatus == "Scrapped");
            TransferCount      = machines.Count(m => m.EquipmentStatus.StartsWith("Transfer"));

            // Événements récents
            var events = await _supabase.GetRecentEventsAsync(10);
            RecentEvents.Clear();
            foreach (var e in events) RecentEvents.Add(e);

            SyncStatus = "✅ Data up to date";
            LastSyncTime = DateTime.Now.ToString("HH:mm:ss");
        }
        catch (Exception ex)
        {
            SyncStatus = $"❌ Error: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  NAVIGATION (noms sans Async pour CommunityToolkit)
    // ══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private async Task GoToScan() =>
        await Shell.Current.GoToAsync("//scan");

    [RelayCommand]
    private async Task GoToMachines() =>
        await Shell.Current.GoToAsync("//machinelist");

    [RelayCommand]
    private async Task GoToAdmin() =>
        await Shell.Current.GoToAsync("//admin");

    [RelayCommand]
    private async Task GoToReport() =>
        await Shell.Current.GoToAsync("//report");

    [RelayCommand]
    private async Task GoToMaintenance() =>
        await Shell.Current.GoToAsync("//maintenance");

    [RelayCommand]
    private async Task SyncNow()
    {
        await LoadDashboard();
    }

    [RelayCommand]
    private async Task Logout()
    {
        bool confirm = await Shell.Current.DisplayAlert(
            "Logout", "Do you want to logout?", "Yes", "No");
        if (!confirm) return;
        await _supabase.LogoutAsync();
        await Shell.Current.GoToAsync("//login");
    }
}
