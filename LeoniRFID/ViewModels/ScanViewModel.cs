using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeoniRFID.Models;
using LeoniRFID.Services;

namespace LeoniRFID.ViewModels;

public partial class ScanViewModel : BaseViewModel
{
    private readonly IRfidService    _rfid;
    private readonly SupabaseService _supabase;

    public ScanViewModel(SupabaseService supabase, IRfidService rfid)
    {
        _supabase = supabase;
        _rfid = rfid;
        Title  = "Scanner RFID";

        _rfid.TagScanned += OnTagScanned;
    }

    [ObservableProperty] private string   _scannedEpc     = string.Empty;
    [ObservableProperty] private string   _manualEpc      = string.Empty;
    [ObservableProperty] private Machine? _foundMachine;
    [ObservableProperty] private bool     _isScanning     = false;
    [ObservableProperty] private bool     _tagNotFound    = false;
    [ObservableProperty] private string   _scanStatusText = "Approchez un tag RFID…";

    public bool HasMachine => FoundMachine is not null;

    [RelayCommand]
    private void StartScan()
    {
        IsScanning     = true;
        TagNotFound    = false;
        FoundMachine   = null;
        ScanStatusText = "Lecture en cours…";
        ScannedEpc     = string.Empty;
        ErrorMessage   = string.Empty;
        _rfid.StartListening();
    }

    [RelayCommand]
    private void StopScan()
    {
        IsScanning     = false;
        ScanStatusText = "Scan arrêté.";
        _rfid.StopListening();
    }

    [RelayCommand]
    private async Task ManualScanAsync()
    {
        if (string.IsNullOrWhiteSpace(ManualEpc)) return;
        _rfid.StopListening();
        await ProcessEpcAsync(ManualEpc.Trim().ToUpperInvariant());
    }

    private async void OnTagScanned(object? sender, string epc)
    {
        _rfid.StopListening();
        IsScanning = false;
        await ProcessEpcAsync(epc);
    }

    private async Task ProcessEpcAsync(string epc)
    {
        IsBusy     = true;
        ScannedEpc = epc;
        ScanStatusText = $"EPC: {epc}";

        try
        {
            FoundMachine = await _supabase.GetMachineByTagIdAsync(epc);
            TagNotFound  = FoundMachine is null;

            if (TagNotFound)
                ScanStatusText = "⚠️ Tag inconnu — non enregistré dans la base.";
            else
                ScanStatusText = $"✅ Machine trouvée : {FoundMachine!.Name}";

            OnPropertyChanged(nameof(HasMachine));

            // Log scan event
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
            }
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task SetStatusAsync(string status)
    {
        if (FoundMachine is null || IsBusy) return;
        IsBusy = true;
        try
        {
            string label = status switch
            {
                "Installed"   => "Installer",
                "Removed"     => "Retirer",
                "Maintenance" => "Mettre en maintenance",
                _             => status
            };

            bool confirm = await Shell.Current.DisplayAlert(
                "Confirmer", $"Confirmer : {label} la machine {FoundMachine.Name} ?", "Oui", "Non");
            if (!confirm) return;

            FoundMachine.Status = status;
            if (status == "Installed")  FoundMachine.InstallationDate = DateTime.Now;
            if (status == "Removed")    FoundMachine.ExitDate         = DateTime.Now;

            await _supabase.SaveMachineAsync(FoundMachine);

            var scanEvent = new ScanEvent
            {
                TagId = FoundMachine.TagId,
                MachineId = FoundMachine.Id,
                UserId = _supabase.CurrentProfile?.Id,
                EventType = status,
                Notes = $"Status changed to {status} from PDA",
                Timestamp = DateTime.UtcNow
            };

            await _supabase.SaveScanEventAsync(scanEvent);

            SetSuccess($"Statut mis à jour : {status}");
            OnPropertyChanged(nameof(FoundMachine));
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ViewDetailAsync()
    {
        if (FoundMachine is null) return;
        await Shell.Current.GoToAsync($"machinedetail?machineId={FoundMachine.Id}");
    }

    public void OnDisappearing() => _rfid.StopListening();
}
