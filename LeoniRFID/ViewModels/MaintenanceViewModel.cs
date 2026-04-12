using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeoniRFID.Helpers;
using LeoniRFID.Models;
using LeoniRFID.Services;
using System.Collections.ObjectModel;

namespace LeoniRFID.ViewModels;

// 🎓 Pédagogie PFE : Page Maintenance dédiée
// Visible uniquement pour le rôle "Maintenance".
// Affiche toutes les machines en panne (Broken) et celles en cours de maintenance (InMaintenance).
// L'agent peut commencer ou terminer une maintenance avec un chronomètre persistant.
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

    // ── Machine sélectionnée pour la maintenance ─────────────────────────
    [ObservableProperty] private Machine? _selectedMachine;
    [ObservableProperty] private bool _showDetail;

    // ── Timer de maintenance (persistant depuis la DB) ───────────────────
    [ObservableProperty] private bool   _isMaintenanceActive;
    [ObservableProperty] private string _maintenanceTimer = "00:00:00";
    private MaintenanceSession? _currentSession;
    private System.Timers.Timer? _timer;

    // ── Compteurs ────────────────────────────────────────────────────────
    [ObservableProperty] private int _brokenCount;
    [ObservableProperty] private int _inMaintenanceCount;
    [ObservableProperty] private bool _canStartMaintenance;
    public bool HasInMaintenanceMachines => InMaintenanceCount > 0;

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
                .Where(m => m.Status == Constants.StatusBroken)
                .OrderBy(m => m.Name).ToList();

            var inMaint = allMachines
                .Where(m => m.Status == Constants.StatusInMaintenance)
                .OrderBy(m => m.Name).ToList();

            BrokenMachines.Clear();
            foreach (var m in broken) BrokenMachines.Add(m);

            InMaintenanceMachines.Clear();
            foreach (var m in inMaint) InMaintenanceMachines.Add(m);

            BrokenCount = broken.Count;
            InMaintenanceCount = inMaint.Count;
        }
        finally { IsBusy = false; }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  SÉLECTION D'UNE MACHINE
    // ══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private async Task SelectMachineAsync(Machine machine)
    {
        if (machine is null) return;
        SelectedMachine = machine;
        ShowDetail = true;

        // Si la machine est déjà en maintenance, reprendre le timer
        StopTimer();
        if (machine.Status == Constants.StatusInMaintenance)
        {
            _currentSession = await _supabase.GetActiveMaintenanceAsync(machine.Id);
            if (_currentSession is not null)
            {
                IsMaintenanceActive = true;
                CanStartMaintenance = false;
                StartTimer();
            }
        }
        else
        {
            IsMaintenanceActive = false;
            CanStartMaintenance = machine.Status == Constants.StatusBroken;
            MaintenanceTimer = "00:00:00";
            _currentSession = null;
        }
    }

    [RelayCommand]
    private void BackToList()
    {
        ShowDetail = false;
        SelectedMachine = null;
        StopTimer();
    }

    // ══════════════════════════════════════════════════════════════════════
    //  COMMENCER LA MAINTENANCE (Broken → InMaintenance)
    // ══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private async Task StartMaintenanceAsync()
    {
        if (SelectedMachine is null || IsBusy) return;
        IsBusy = true;
        try
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Commencer Maintenance",
                $"Confirmer : Commencer la maintenance de {SelectedMachine.Name} ?",
                "Oui", "Non");
            if (!confirm) return;

            // 1. Changer le statut de la machine
            SelectedMachine.Status = Constants.StatusInMaintenance;
            await _supabase.SaveMachineAsync(SelectedMachine);

            // 2. Créer la session de maintenance (démarre le chrono en DB)
            _currentSession = await _supabase.StartMaintenanceAsync(
                SelectedMachine.Id, _supabase.CurrentProfile?.Id);

            // 3. Traçabilité
            var scanEvent = new ScanEvent
            {
                TagId = SelectedMachine.TagId,
                MachineId = SelectedMachine.Id,
                UserId = _supabase.CurrentProfile?.Id,
                EventType = Constants.StatusInMaintenance,
                Notes = "Maintenance démarrée depuis la page Maintenance",
                Timestamp = DateTime.UtcNow
            };
            await _supabase.SaveScanEventAsync(scanEvent);

            // 4. Démarrer le timer visuel
            StartTimer();

            await Shell.Current.DisplayAlert("Succès", 
                $"🔧 Maintenance de {SelectedMachine.Name} démarrée !\nLe chronomètre tourne.", "OK");

            // 5. Rafraîchir les listes
            await LoadAsync();

            // Ré-sélectionner pour afficher le timer
            OnPropertyChanged(nameof(SelectedMachine));
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Erreur", ex.Message, "OK");
        }
        finally { IsBusy = false; }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  TERMINER LA MAINTENANCE (InMaintenance → Running)
    // ══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private async Task EndMaintenanceAsync()
    {
        if (SelectedMachine is null || _currentSession is null || IsBusy) return;
        IsBusy = true;
        try
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Terminer Maintenance",
                $"Confirmer : Terminer la maintenance de {SelectedMachine.Name} ?\n\nDurée : {MaintenanceTimer}",
                "Oui", "Non");
            if (!confirm) return;

            // 1. Stopper le timer
            StopTimer();

            // 2. Clôturer la session en DB
            await _supabase.EndMaintenanceAsync(_currentSession);

            // 3. Remettre la machine en marche
            SelectedMachine.Status = Constants.StatusRunning;
            await _supabase.SaveMachineAsync(SelectedMachine);

            // 4. Traçabilité
            var scanEvent = new ScanEvent
            {
                TagId = SelectedMachine.TagId,
                MachineId = SelectedMachine.Id,
                UserId = _supabase.CurrentProfile?.Id,
                EventType = Constants.StatusRunning,
                Notes = $"Maintenance terminée — Durée : {_currentSession.DurationDisplay}",
                Timestamp = DateTime.UtcNow
            };
            await _supabase.SaveScanEventAsync(scanEvent);

            await Shell.Current.DisplayAlert("Succès",
                $"✅ Maintenance terminée !\n\nMachine : {SelectedMachine.Name}\nDurée : {_currentSession.DurationDisplay}", "OK");

            _currentSession = null;
            ShowDetail = false;
            SelectedMachine = null;

            // 5. Rafraîchir
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Erreur", ex.Message, "OK");
        }
        finally { IsBusy = false; }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  TIMER PERSISTANT (même logique que ScanViewModel)
    // ══════════════════════════════════════════════════════════════════════

    private void StartTimer()
    {
        IsMaintenanceActive = true;
        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += (s, e) => UpdateTimerDisplay();
        _timer.Start();
        UpdateTimerDisplay();
    }

    private void StopTimer()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
        IsMaintenanceActive = false;
        MaintenanceTimer = "00:00:00";
    }

    private void UpdateTimerDisplay()
    {
        if (_currentSession is null) return;
        var elapsed = DateTime.UtcNow - _currentSession.StartedAt;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            MaintenanceTimer = elapsed.ToString(@"hh\:mm\:ss");
        });
    }

    public void OnDisappearing()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
    }
}
