using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeoniRFID.Models;
using LeoniRFID.Services;
using System.Collections.ObjectModel;

namespace LeoniRFID.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly DatabaseService _db;
    private readonly AuthService     _auth;
    private readonly SyncService     _sync;

    public DashboardViewModel(DatabaseService db, AuthService auth, SyncService sync)
    {
        _db   = db;
        _auth = auth;
        _sync = sync;
        Title = "Tableau de bord";
        _sync.SyncStatusChanged += (_, msg) => SyncStatus = msg;
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

    // ── Sync ──────────────────────────────────────────────────────────────────
    [ObservableProperty] private string _syncStatus = string.Empty;
    [ObservableProperty] private string _lastSyncTime = "Jamais";

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await _db.InitAsync();

            // User
            var user = _auth.CurrentUser;
            if (user is not null)
            {
                UserName     = user.FullName;
                UserRole     = user.RoleDisplay;
                UserInitials = user.Initials;
                IsAdmin      = user.IsAdmin;
            }

            // Stats
            var machines = await _db.GetAllMachinesAsync();
            TotalMachines    = machines.Count;
            InstalledCount   = machines.Count(m => m.Status == "Installed");
            RemovedCount     = machines.Count(m => m.Status == "Removed");
            MaintenanceCount = machines.Count(m => m.Status == "Maintenance");
            Ltn1Count        = machines.Count(m => m.Department == "LTN1");
            Ltn2Count        = machines.Count(m => m.Department == "LTN2");
            Ltn3Count        = machines.Count(m => m.Department == "LTN3");

            // Recent events
            var events = await _db.GetRecentEventsAsync(15);
            RecentEvents.Clear();
            foreach (var e in events) RecentEvents.Add(e);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task SyncNowAsync()
    {
        SyncStatus = "Synchronisation…";
        await _sync.SyncAsync();
        LastSyncTime = DateTime.Now.ToString("HH:mm:ss");
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
        _auth.Logout();
        await Shell.Current.GoToAsync("//login");
    }
}
