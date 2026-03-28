using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeoniRFID.Models;
using LeoniRFID.Services;
using System.Collections.ObjectModel;

namespace LeoniRFID.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly SupabaseService _supabase;

    public DashboardViewModel(SupabaseService supabase)
    {
        _supabase = supabase;
        Title = "Tableau de Bord";
    }

    // ── User info ─────────────────────────────────────────────────────────────
    [ObservableProperty] private string _userName     = string.Empty;
    [ObservableProperty] private string _userRole     = string.Empty;
    [ObservableProperty] private string _userInitials = string.Empty;
    [ObservableProperty] private bool   _isAdmin      = false;

    // ── Stats ─────────────────────────────────────────────────────────────────
    [ObservableProperty] private int _totalMachines;
    [ObservableProperty] private int _installedCount;
    [ObservableProperty] private int _removedCount;
    [ObservableProperty] private int _maintenanceCount;

    // ── Per-department stats ───────────────────────────────────────────────────
    [ObservableProperty] private int _ltn1Count;
    [ObservableProperty] private int _ltn2Count;
    [ObservableProperty] private int _ltn3Count;

    // ── Recent events ─────────────────────────────────────────────────────────
    public ObservableCollection<ScanEvent> RecentEvents { get; } = [];

    // ── Sync (Cloud direct, pas de sync offline) ──────────────────────────────
    [ObservableProperty] private string _syncStatus = "Cloud connecté";
    [ObservableProperty] private string _lastSyncTime = "—";

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            // User
            IsAdmin = _supabase.IsAdmin;
            UserName = _supabase.CurrentProfile?.FullName ?? "Utilisateur";
            UserRole = _supabase.CurrentProfile?.RoleDisplay ?? "Inconnu";
            UserInitials = _supabase.CurrentProfile?.Initials ?? "?";

            // Stats
            var machines = await _supabase.GetAllMachinesAsync();
            TotalMachines    = machines.Count;
            InstalledCount   = machines.Count(m => m.Status == "Installed");
            RemovedCount     = machines.Count(m => m.Status == "Removed");
            MaintenanceCount = machines.Count(m => m.Status == "Maintenance");
            Ltn1Count        = machines.Count(m => m.Department == "LTN1");
            Ltn2Count        = machines.Count(m => m.Department == "LTN2");
            Ltn3Count        = machines.Count(m => m.Department == "LTN3");

            // Recent events
            var events = await _supabase.GetRecentEventsAsync(10);
            RecentEvents.Clear();
            foreach (var e in events) RecentEvents.Add(e);

            SyncStatus = "✅ Données à jour";
            LastSyncTime = DateTime.Now.ToString("HH:mm:ss");
        }
        catch (Exception ex)
        {
            SyncStatus = $"❌ Erreur : {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task SyncNowAsync()
    {
        // En mode BaaS, un "Sync" revient simplement à recharger les données du cloud
        await LoadAsync();
    }

    [RelayCommand]
    private async Task GoToScanAsync() =>
        await Shell.Current.GoToAsync("//scan");

    [RelayCommand]
    private async Task GoToAdminAsync() =>
        await Shell.Current.GoToAsync("//admin");

    [RelayCommand]
    private async Task GoToReportAsync() =>
        await Shell.Current.GoToAsync("//report");

    [RelayCommand]
    private async Task LogoutAsync()
    {
        bool confirm = await Shell.Current.DisplayAlert(
            "Déconnexion", "Voulez-vous vous déconnecter ?", "Oui", "Non");
        if (!confirm) return;
        await _supabase.LogoutAsync();
        await Shell.Current.GoToAsync("//login");
    }
}
