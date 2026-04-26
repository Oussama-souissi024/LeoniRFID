using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeoniRFID.Helpers;
using LeoniRFID.Models;
using LeoniRFID.Services;
using System.Timers;

namespace LeoniRFID.ViewModels;

// 🎓 Pédagogie PFE : Page Session de Maintenance
// Cette page s'ouvre quand l'agent clique sur une machine (en panne ou en cours).
// Elle affiche les détails complets de la machine, le chronomètre persistant,
// et les boutons d'action (Commencer / Terminer la maintenance).
// La séparation en page dédiée suit le principe "Single Responsibility" (SRP).

[QueryProperty(nameof(MachineId), "machineId")]
public partial class MaintenanceSessionViewModel : BaseViewModel
{
    private readonly SupabaseService _supabase;

    public MaintenanceSessionViewModel(SupabaseService supabase)
    {
        _supabase = supabase;
        Title = "Maintenance Session";
    }

    // ── Paramètre de navigation (ID machine passé par QueryProperty) ──
    [ObservableProperty] private int _machineId;

    // ── Machine chargée ───────────────────────────────────────────────
    [ObservableProperty] private Machine? _machine;

    // ── Timer de maintenance ──────────────────────────────────────────
    [ObservableProperty] private bool   _isMaintenanceActive;
    [ObservableProperty] private string _maintenanceTimer = "00:00:00";
    [ObservableProperty] private bool   _canStartMaintenance;
    [ObservableProperty] private string _statusLabel = "";
    [ObservableProperty] private Color  _statusColor = Colors.Gray;
    private MaintenanceSession? _currentSession;
    private System.Timers.Timer? _timer;

    // ══════════════════════════════════════════════════════════════════════
    //  CHARGEMENT : appelé quand la page s'affiche
    // ══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy || MachineId <= 0) return;
        IsBusy = true;
        try
        {
            // 1. Charger la machine depuis Supabase
            var allMachines = await _supabase.GetAllMachinesAsync();
            Machine = allMachines.FirstOrDefault(m => m.Id == MachineId);

            if (Machine is null)
            {
                await Shell.Current.DisplayAlert("Error", "Machine not found.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            Title = Machine.StandardEquipmentName;

            // 2. Déterminer l'état et configurer l'UI
            StopTimer();
            if (Machine.EquipmentStatus == Constants.StatusInMaintenance)
            {
                // Machine déjà en maintenance → reprendre le chrono
                _currentSession = await _supabase.GetActiveMaintenanceAsync(Machine.Id);
                if (_currentSession is not null)
                {
                    IsMaintenanceActive = true;
                    CanStartMaintenance = false;
                    StatusLabel = "🔧 Maintenance in progress";
                    StatusColor = Color.FromArgb("#F39C12");
                    StartTimer();
                }
            }
            else if (Machine.EquipmentStatus == Constants.StatusBroken)
            {
                // Machine en panne → peut démarrer la maintenance
                IsMaintenanceActive = false;
                CanStartMaintenance = true;
                MaintenanceTimer = "00:00:00";
                StatusLabel = "🔴 Defect — Ready for intervention";
                StatusColor = Color.FromArgb("#E74C3C");
                _currentSession = null;
            }
            else
            {
                // Autre statut → lecture seule
                IsMaintenanceActive = false;
                CanStartMaintenance = false;
                StatusLabel = $"📊 Status: {Machine.StatusDisplay}";
                StatusColor = Color.FromArgb("#27AE60");
                _currentSession = null;
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally { IsBusy = false; }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  COMMENCER LA MAINTENANCE (Defect → InMaintenance)
    // ══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private async Task StartMaintenanceAsync()
    {
        if (Machine is null || IsBusy) return;
        IsBusy = true;
        try
        {
            // 🎓 Saisie des notes de diagnostic terrain
            var notes = await Shell.Current.DisplayPromptAsync(
                "🔧 Start Maintenance",
                $"Machine: {Machine.StandardEquipmentName}\n\nDescribe the observed problem:",
                "Start", "Cancel",
                placeholder: "Ex: Blocked motor, abnormal noise...",
                maxLength: 500);

            if (notes is null) return; // Annulé

            // 1. Changer le statut
            Machine.EquipmentStatus = Constants.StatusInMaintenance;
            await _supabase.SaveMachineAsync(Machine);

            // 2. Créer la session de maintenance en DB
            _currentSession = await _supabase.StartMaintenanceAsync(
                Machine.Id, _supabase.CurrentProfile?.Id);

            if (!string.IsNullOrWhiteSpace(notes))
                _currentSession.Notes = notes;

            // 3. Traçabilité
            var scanEvent = new ScanEvent
            {
                TagId = Machine.TagReference,
                MachineId = Machine.Id,
                UserId = _supabase.CurrentProfile?.Id,
                EventType = Constants.StatusInMaintenance,
                Notes = $"Maintenance started — {(string.IsNullOrWhiteSpace(notes) ? "No notes" : notes)}",
                Timestamp = DateTime.UtcNow
            };
            await _supabase.SaveScanEventAsync(scanEvent);

            // 4. Démarrer le timer visuel
            StartTimer();
            CanStartMaintenance = false;
            StatusLabel = "🔧 Maintenance in progress";
            StatusColor = Color.FromArgb("#F39C12");
            OnPropertyChanged(nameof(Machine));

            await Shell.Current.DisplayAlert("Success",
                $"🔧 Maintenance for {Machine.StandardEquipmentName} started!\nTimer is running.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally { IsBusy = false; }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  TERMINER LA MAINTENANCE (InMaintenance → Active)
    // ══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private async Task EndMaintenanceAsync()
    {
        if (Machine is null || _currentSession is null || IsBusy) return;
        IsBusy = true;
        try
        {
            // 🎓 Saisie des notes de clôture
            var closingNotes = await Shell.Current.DisplayPromptAsync(
                "✅ End Maintenance",
                $"Machine: {Machine.StandardEquipmentName}\nDuration: {MaintenanceTimer}\n\nDescribe the performed intervention:",
                "End", "Cancel",
                placeholder: "Ex: Belt replacement, recalibration...",
                maxLength: 500);

            if (closingNotes is null) return; // Annulé

            // 1. Stopper le timer
            StopTimer();

            // 2. Clôturer la session en DB avec les notes
            var startNotes = _currentSession.Notes ?? "";
            _currentSession.Notes = string.IsNullOrWhiteSpace(startNotes)
                ? $"Closing: {closingNotes}"
                : $"{startNotes} | Closing: {closingNotes}";
            await _supabase.EndMaintenanceAsync(_currentSession);

            // 3. Remettre la machine en Active
            Machine.EquipmentStatus = Constants.StatusActive;
            await _supabase.SaveMachineAsync(Machine);

            // 4. Traçabilité
            var scanEvent = new ScanEvent
            {
                TagId = Machine.TagReference,
                MachineId = Machine.Id,
                UserId = _supabase.CurrentProfile?.Id,
                EventType = Constants.StatusActive,
                Notes = $"Maintenance ended — Duration: {_currentSession.DurationDisplay} — {closingNotes}",
                Timestamp = DateTime.UtcNow
            };
            await _supabase.SaveScanEventAsync(scanEvent);

            await Shell.Current.DisplayAlert("Success",
                $"✅ Maintenance ended!\n\nMachine: {Machine.StandardEquipmentName}\nDuration: {_currentSession.DurationDisplay}", "OK");

            _currentSession = null;

            // 5. Retourner à la liste de maintenance
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally { IsBusy = false; }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  RETOUR
    // ══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private async Task GoBackAsync()
    {
        StopTimer();
        await Shell.Current.GoToAsync("..");
    }

    // ══════════════════════════════════════════════════════════════════════
    //  TIMER PERSISTANT
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
    }

    private void UpdateTimerDisplay()
    {
        if (_currentSession is null) return;
        // ⚠️ StartedAt revient de Supabase en heure locale → convertir en UTC
        var startUtc = _currentSession.StartedAt.ToUniversalTime();
        var elapsed = DateTime.UtcNow - startUtc;
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
