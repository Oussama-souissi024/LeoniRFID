using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeoniRFID.Helpers;
using LeoniRFID.Models;
using LeoniRFID.Services;

namespace LeoniRFID.ViewModels;

// 🎓 Pédagogie PFE : Le Scanner RFID (Cœur Métier de l'Application)
// Ce ViewModel gère le flux complet du scan RFID avec le workflow de maintenance :
// - Technicien : peut signaler une panne (Running → Broken)
// - Agent Maintenance : peut commencer/terminer la maintenance (Broken → InMaintenance → Running)
// - Le timer est persistant : calculé depuis started_at en base de données
public partial class ScanViewModel : BaseViewModel
{
    private readonly IRfidService    _rfid;
    private readonly SupabaseService _supabase;

    public ScanViewModel(SupabaseService supabase, IRfidService rfid)
    {
        _supabase = supabase;
        _rfid = rfid;
        Title  = "RFID Scanner";
        _rfid.TagScanned += OnTagScanned;
    }

    // ── Propriétés Observables (liées au XAML par Data Binding) ──────────

    [ObservableProperty] private string   _scannedEpc     = string.Empty;
    [ObservableProperty] private string   _manualEpc      = string.Empty;
    [ObservableProperty] private Machine? _foundMachine;
    [ObservableProperty] private bool     _isScanning     = false;
    [ObservableProperty] private bool     _tagNotFound    = false;
    [ObservableProperty] private string   _scanStatusText = "Bring an RFID tag closer...";

    // ── Rôle de l'utilisateur connecté ──────────────────────────────────
    [ObservableProperty] private bool _isTechnician;
    [ObservableProperty] private bool _isMaintenanceAgent;

    // ── Visibilité des boutons conditionnels selon le rôle et le statut ─
    [ObservableProperty] private bool _canReportBroken;       // Technicien + machine Running
    [ObservableProperty] private bool _canStartMaintenance;   // Maintenance + machine Broken
    [ObservableProperty] private bool _canEndMaintenance;     // Maintenance + machine InMaintenance

    // ── Timer de maintenance (persistant depuis la DB) ──────────────────
    [ObservableProperty] private bool   _isMaintenanceActive;
    [ObservableProperty] private string _maintenanceTimer = "00:00:00";
    private MaintenanceSession? _currentSession;
    private System.Timers.Timer? _timer;

    // ── Formulaire d'enregistrement nouvelle machine ────────────────────
    [ObservableProperty] private bool   _showRegisterForm;
    [ObservableProperty] private string _newMachineName       = string.Empty;
    [ObservableProperty] private string _selectedPlant        = "LTN1";
    public List<string> PlantOptions { get; } = ["MH", "SB", "MS", "MN", "LTN1", "LTN2", "LTN3"];

    public bool HasMachine => FoundMachine is not null;

    // ── Commandes de scan ────────────────────────────────────────────────

    [RelayCommand]
    private void StartScan()
    {
        IsScanning     = true;
        TagNotFound    = false;
        FoundMachine   = null;
        ScanStatusText = "Reading...";
        ScannedEpc     = string.Empty;
        ErrorMessage   = string.Empty;
        StopTimer();
        UpdateButtonVisibility();
        _rfid.StartListening();
    }

    [RelayCommand]
    private void StopScan()
    {
        IsScanning     = false;
        ScanStatusText = "Scan stopped.";
        _rfid.StopListening();
    }

    [RelayCommand]
    private async Task ManualScanAsync()
    {
        if (string.IsNullOrWhiteSpace(ManualEpc)) return;
        _rfid.StopListening();
        await ProcessEpcAsync(ManualEpc.Trim().ToUpperInvariant());
    }

    // ── Callback déclenché automatiquement par le matériel Zebra ────────

    private async void OnTagScanned(object? sender, string epc)
    {
        _rfid.StopListening();
        IsScanning = false;
        await ProcessEpcAsync(epc);
    }

    // ── Logique Métier : traitement de l'EPC scanné ─────────────────────

    private async Task ProcessEpcAsync(string epc)
    {
        IsBusy     = true;
        ScannedEpc = epc;
        ScanStatusText = $"EPC: {epc}";

        try
        {
            // 1. Déterminer le rôle de l'utilisateur connecté
            IsTechnician       = _supabase.IsTechnician;
            IsMaintenanceAgent = _supabase.IsMaintenance;

            // 2. Chercher la machine dans Supabase via son tag RFID
            FoundMachine = await _supabase.GetMachineByTagReferenceAsync(epc);
            TagNotFound  = FoundMachine is null;

            if (TagNotFound)
            {
                ScanStatusText = "🆕 Unknown tag — Register as a new machine?";
                ShowRegisterForm = true;
                NewMachineName = string.Empty;
            }
            else
            {
                ScanStatusText = $"✅ Machine found: {FoundMachine!.StandardEquipmentName}";
                ShowRegisterForm = false;
            }

            OnPropertyChanged(nameof(HasMachine));

            // 3. Enregistrement de la Traçabilité (ScanEvent)
            if (FoundMachine is not null)
            {
                var scanEvent = new ScanEvent
                {
                    TagId = epc,
                    MachineId = FoundMachine.Id,
                    UserId = _supabase.CurrentProfile?.Id,
                    EventType = "Scan",
                    Timestamp = DateTime.UtcNow
                };
                await _supabase.SaveScanEventAsync(scanEvent);

                // 4. Si une maintenance est en cours, reprendre le timer
                if (FoundMachine.EquipmentStatus == Constants.StatusInMaintenance)
                {
                    await ResumeTimerFromDbAsync();
                }

                // 5. 🔧 Proposition automatique de maintenance
                // Si l'agent de maintenance scanne une machine en Defect,
                // proposer directement de commencer la maintenance
                if (IsMaintenanceAgent && FoundMachine.EquipmentStatus == Constants.StatusDefect)
                {
                    bool startMaint = await Shell.Current.DisplayAlert(
                        "🔧 Defective machine detected",
                        $"{FoundMachine.StandardEquipmentName}\n\nThis machine is in Defect status.\nDo you want to start maintenance?",
                        "Yes, start", "No");

                    if (startMaint)
                    {
                        // Naviguer vers la page Maintenance
                        await Shell.Current.GoToAsync("//maintenance");
                        return;
                    }
                }
            }

            // 6. Mettre à jour la visibilité des boutons
            UpdateButtonVisibility();
        }
        finally { IsBusy = false; }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  ENREGISTRER UNE NOUVELLE MACHINE (Tag inconnu)
    // ══════════════════════════════════════════════════════════════════════

    // 🎓 Pédagogie PFE : Quand un tag RFID est scanné et qu'il n'existe pas
    // dans la base de données, l'utilisateur peut enregistrer une nouvelle machine.
    // La machine est créée avec le statut "Paused" (En Pause).
    [RelayCommand]
    private async Task RegisterMachineAsync()
    {
        if (string.IsNullOrWhiteSpace(NewMachineName))
        {
            await Shell.Current.DisplayAlert("Required field", "Please enter the machine name.", "OK");
            return;
        }
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var newMachine = new Machine
            {
                TagReference = ScannedEpc,
                StandardEquipmentName = NewMachineName.Trim(),
                Plant = SelectedPlant ?? "LTN1",
                EquipmentStatus = Constants.StatusPassive,
                YearOfConstruction = DateTime.Now.Year
            };

            await _supabase.SaveMachineAsync(newMachine);

            // Recharger la machine depuis Supabase (pour récupérer l'ID auto-généré)
            FoundMachine = await _supabase.GetMachineByTagReferenceAsync(ScannedEpc);
            TagNotFound = false;
            ShowRegisterForm = false;
            OnPropertyChanged(nameof(HasMachine));

            // Traçabilité
            if (FoundMachine is not null)
            {
                var scanEvent = new ScanEvent
                {
                    TagId = ScannedEpc,
                    MachineId = FoundMachine.Id,
                    UserId = _supabase.CurrentProfile?.Id,
                    EventType = "Registered",
                    Notes = $"New machine registered: {NewMachineName}",
                    Timestamp = DateTime.UtcNow
                };
                await _supabase.SaveScanEventAsync(scanEvent);
            }

            ScanStatusText = $"✅ Machine '{NewMachineName}' registered!";
            await Shell.Current.DisplayAlert("Success", $"Machine '{NewMachineName}' registered with ⏸️ Passive status.", "OK");
            UpdateButtonVisibility();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to register: {ex.Message}", "OK");
        }
        finally { IsBusy = false; }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  ACTIONS PAR RÔLE
    // ══════════════════════════════════════════════════════════════════════

    // 🎓 TECHNICIEN : Signaler une panne (Running → Broken)
    [RelayCommand]
    private async Task ReportBrokenAsync()
    {
        if (FoundMachine is null || IsBusy) return;
        IsBusy = true;
        try
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Report Defect",
                $"Confirm: Report machine {FoundMachine.Name} as defective?",
                "Yes", "No");
            if (!confirm) return;

            FoundMachine.EquipmentStatus = Constants.StatusBroken;
            await _supabase.SaveMachineAsync(FoundMachine);

            var scanEvent = new ScanEvent
            {
                TagId = FoundMachine.TagReference,
                MachineId = FoundMachine.Id,
                UserId = _supabase.CurrentProfile?.Id,
                EventType = Constants.StatusBroken,
                Notes = "Machine reported as defective by technician",
                Timestamp = DateTime.UtcNow
            };
            await _supabase.SaveScanEventAsync(scanEvent);

            SetSuccess("🔴 Machine reported as defective");
            OnPropertyChanged(nameof(FoundMachine));
            UpdateButtonVisibility();
        }
        finally { IsBusy = false; }
    }

    // 🎓 AGENT MAINTENANCE : Commencer la maintenance (Broken → InMaintenance)
    [RelayCommand]
    private async Task StartMaintenanceAsync()
    {
        if (FoundMachine is null || IsBusy) return;
        IsBusy = true;
        try
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Start Maintenance",
                $"Confirm: Start maintenance for {FoundMachine.Name}?",
                "Yes", "No");
            if (!confirm) return;

            // 1. Changer le statut de la machine
            FoundMachine.EquipmentStatus = Constants.StatusInMaintenance;
            await _supabase.SaveMachineAsync(FoundMachine);

            // 2. Créer la session de maintenance (démarre le chrono en DB)
            _currentSession = await _supabase.StartMaintenanceAsync(
                FoundMachine.Id, _supabase.CurrentProfile?.Id);

            // 3. Enregistrer la traçabilité
            var scanEvent = new ScanEvent
            {
                TagId = FoundMachine.TagReference,
                MachineId = FoundMachine.Id,
                UserId = _supabase.CurrentProfile?.Id,
                EventType = Constants.StatusInMaintenance,
                Notes = "Maintenance started",
                Timestamp = DateTime.UtcNow
            };
            await _supabase.SaveScanEventAsync(scanEvent);

            // 4. Démarrer le timer visuel
            StartTimer();

            SetSuccess("🔧 Maintenance in progress — Timer started");
            OnPropertyChanged(nameof(FoundMachine));
            UpdateButtonVisibility();
        }
        finally { IsBusy = false; }
    }

    // 🎓 AGENT MAINTENANCE : Terminer la maintenance (InMaintenance → Running)
    [RelayCommand]
    private async Task EndMaintenanceAsync()
    {
        if (FoundMachine is null || _currentSession is null || IsBusy) return;
        IsBusy = true;
        try
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "End Maintenance",
                $"Confirm: End maintenance for {FoundMachine.Name}?",
                "Yes", "No");
            if (!confirm) return;

            // 1. Arrêter le timer
            StopTimer();

            // 2. Clôturer la session en DB (calcule la durée)
            await _supabase.EndMaintenanceAsync(_currentSession);

            // 3. Remettre la machine en marche
            FoundMachine.EquipmentStatus = Constants.StatusRunning;
            await _supabase.SaveMachineAsync(FoundMachine);

            // 4. Traçabilité
            var scanEvent = new ScanEvent
            {
                TagId = FoundMachine.TagReference,
                MachineId = FoundMachine.Id,
                UserId = _supabase.CurrentProfile?.Id,
                EventType = Constants.StatusRunning,
                Notes = $"Maintenance ended — Duration: {_currentSession.DurationDisplay}",
                Timestamp = DateTime.UtcNow
            };
            await _supabase.SaveScanEventAsync(scanEvent);

            SetSuccess($"✅ Maintenance ended — Duration: {_currentSession.DurationDisplay}");
            _currentSession = null;
            OnPropertyChanged(nameof(FoundMachine));
            UpdateButtonVisibility();
        }
        finally { IsBusy = false; }
    }

    // ── Ancien bouton SetStatus (conservé pour compatibilité Admin) ──────

    [RelayCommand]
    private async Task SetStatusAsync(string status)
    {
        if (FoundMachine is null || IsBusy) return;
        IsBusy = true;
        try
        {
            string label = status switch
            {
                "Running"       => "Restart",
                "Removed"       => "Remove",
                _               => status
            };

            bool confirm = await Shell.Current.DisplayAlert(
                "Confirm", $"Confirm: {label} machine {FoundMachine.Name}?", "Yes", "No");
            if (!confirm) return;

            FoundMachine.EquipmentStatus = status;
            if (status == "Scrapped") { /* No exit_date needed anymore */ }

            await _supabase.SaveMachineAsync(FoundMachine);

            var scanEvent = new ScanEvent
            {
                TagId = FoundMachine.TagReference,
                MachineId = FoundMachine.Id,
                UserId = _supabase.CurrentProfile?.Id,
                EventType = status,
                Notes = $"Status changed to {status} from PDA",
                Timestamp = DateTime.UtcNow
            };
            await _supabase.SaveScanEventAsync(scanEvent);

            SetSuccess($"Status updated: {status}");
            OnPropertyChanged(nameof(FoundMachine));
            UpdateButtonVisibility();
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ViewDetailAsync()
    {
        if (FoundMachine is null) return;
        await Shell.Current.GoToAsync($"machinedetail?machineId={FoundMachine.Id}");
    }

    // ══════════════════════════════════════════════════════════════════════
    //  TIMER DE MAINTENANCE (Persistant depuis la DB)
    // ══════════════════════════════════════════════════════════════════════

    // 🎓 Pédagogie PFE : Timer Persistant
    // Le timer n'utilise PAS un DateTime local. Il calcule toujours
    // la différence entre DateTime.UtcNow et _currentSession.StartedAt (en DB).
    // Ainsi, si l'app est fermée et rouverte, le timer reprend là où il était.

    private void StartTimer()
    {
        IsMaintenanceActive = true;
        _timer = new System.Timers.Timer(1000); // Tick toutes les secondes
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
        // ⚠️ StartedAt revient de Supabase en heure locale → convertir en UTC
        var startUtc = _currentSession.StartedAt.ToUniversalTime();
        var elapsed = DateTime.UtcNow - startUtc;
        // 🎓 MainThread.BeginInvokeOnMainThread est nécessaire car le Timer
        // tourne sur un thread secondaire, et le XAML ne peut être mis à jour
        // que depuis le thread principal (UI Thread).
        MainThread.BeginInvokeOnMainThread(() =>
        {
            MaintenanceTimer = elapsed.ToString(@"hh\:mm\:ss");
        });
    }

    // 🎓 Reprise du timer depuis la DB (si l'app a été fermée pendant une maintenance)
    private async Task ResumeTimerFromDbAsync()
    {
        if (FoundMachine is null) return;
        _currentSession = await _supabase.GetActiveMaintenanceAsync(FoundMachine.Id);
        if (_currentSession is not null)
        {
            StartTimer();
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  VISIBILITÉ DES BOUTONS (selon le rôle et le statut)
    // ══════════════════════════════════════════════════════════════════════

    private void UpdateButtonVisibility()
    {
        var status = FoundMachine?.EquipmentStatus;

        // Technicien : peut signaler une panne seulement si la machine est en marche
        CanReportBroken = IsTechnician && status == Constants.StatusRunning;

        // Agent Maintenance : peut commencer si la machine est en panne
        CanStartMaintenance = IsMaintenanceAgent && status == Constants.StatusBroken;

        // Agent Maintenance : peut terminer si la maintenance est en cours
        CanEndMaintenance = IsMaintenanceAgent && status == Constants.StatusInMaintenance;
    }

    // 🎓 Très important : libérer le matériel et le timer quand la page disparaît
    public void OnDisappearing()
    {
        _rfid.StopListening();
        // Note : on ne stoppe PAS le timer ici ! La maintenance continue
        // même si on quitte la page. Le timer sera repris via ResumeTimerFromDbAsync
        // quand l'agent revient sur la page et re-scanne la machine.
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
    }
}
