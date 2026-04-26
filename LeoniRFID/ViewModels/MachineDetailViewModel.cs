using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeoniRFID.Helpers;
using LeoniRFID.Models;
using LeoniRFID.Services;
using System.Collections.ObjectModel;

namespace LeoniRFID.ViewModels;

// 🎓 Pédagogie PFE : Navigation avec Paramètres (QueryProperty)
// L'attribut [QueryProperty] permet de recevoir automatiquement un paramètre
// depuis l'URL de navigation. Quand on navigue vers "machinedetail?machineId=5",
// .NET MAUI remplit automatiquement la propriété MachineId avec la valeur 5.
[QueryProperty(nameof(MachineId), "machineId")]
public partial class MachineDetailViewModel : BaseViewModel
{
    private readonly SupabaseService _supabase;

    public MachineDetailViewModel(SupabaseService supabase)
    {
        _supabase = supabase;
        Title = "Machine Details";
    }

    [ObservableProperty] private int      _machineId;
    [ObservableProperty] private Machine? _machine;
    [ObservableProperty] private bool     _isEditing  = false;
    [ObservableProperty] private bool     _isAdmin    = false;
    [ObservableProperty] private bool     _canChangeStatus = false;

    public ObservableCollection<ScanEvent> Events { get; } = [];

    // Editable fields
    [ObservableProperty] private string _editName       = string.Empty;
    [ObservableProperty] private string _editPlant      = string.Empty;
    [ObservableProperty] private string _editStatus     = string.Empty;
    [ObservableProperty] private string _editNotes      = string.Empty;

    // 🎓 Liste des statuts disponibles pour le changement rapide
    public List<string> AvailableStatuses { get; } =
    [
        Constants.StatusActive,       // En marche
        Constants.StatusPassive,      // En pause
        Constants.StatusBroken        // En panne (Defect)
    ];

    partial void OnMachineIdChanged(int value) =>
        MainThread.BeginInvokeOnMainThread(async () => await LoadAsync());

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy  = true;
        IsAdmin = _supabase.IsAdmin;
        // 🎓 Seuls Admin et Technician peuvent changer le statut
        CanChangeStatus = _supabase.IsAdmin || _supabase.IsTechnician;
        try
        {
            Machine = await _supabase.GetMachineByIdAsync(MachineId);
            if (Machine is null) return;

            Title        = Machine.StandardEquipmentName;
            EditName       = Machine.StandardEquipmentName;
            EditPlant      = Machine.Plant;
            EditStatus     = Machine.EquipmentStatus;
            EditNotes      = Machine.Notes ?? string.Empty;

            var events = await _supabase.GetEventsByMachineAsync(MachineId);
            Events.Clear();
            foreach (var e in events) Events.Add(e);
        }
        finally { IsBusy = false; }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  CHANGEMENT RAPIDE DE STATUT (Admin + Technician)
    // ══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private async Task ChangeStatusAsync(string newStatus)
    {
        if (Machine is null || !CanChangeStatus || string.IsNullOrEmpty(newStatus)) return;
        if (Machine.EquipmentStatus == newStatus) return; // Déjà ce statut

        bool confirm = await Shell.Current.DisplayAlert(
            "Change status",
            $"Change « {Machine.StandardEquipmentName} » from\n{Machine.StatusDisplay} → {newStatus}?",
            "Confirm", "Cancel");

        if (!confirm) return;

        IsBusy = true;
        try
        {
            var oldStatus = Machine.EquipmentStatus;
            Machine.EquipmentStatus = newStatus;
            await _supabase.SaveMachineAsync(Machine);

            // Traçabilité : enregistrer l'événement
            var scanEvent = new ScanEvent
            {
                TagId = Machine.TagReference,
                MachineId = Machine.Id,
                UserId = _supabase.CurrentProfile?.Id,
                EventType = newStatus,
                Notes = $"Status changed: {oldStatus} → {newStatus}",
                Timestamp = DateTime.UtcNow
            };
            await _supabase.SaveScanEventAsync(scanEvent);

            await Shell.Current.DisplayAlert("Success",
                $"✅ Status updated: {newStatus}", "OK");

            await LoadAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void StartEdit()
    {
        if (!IsAdmin) return;
        IsEditing = true;
    }

    [RelayCommand]
    private async Task SaveEditAsync()
    {
        if (Machine is null) return;
        IsBusy = true;
        try
        {
            Machine.StandardEquipmentName = EditName;
            Machine.Plant          = EditPlant;
            Machine.EquipmentStatus = EditStatus;
            Machine.Notes          = EditNotes;
            await _supabase.SaveMachineAsync(Machine);
            IsEditing = false;
            SetSuccess("Machine updated.");
            await LoadAsync();
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        if (Machine is null) return;
        EditName       = Machine.StandardEquipmentName;
        EditPlant      = Machine.Plant;
        EditStatus     = Machine.EquipmentStatus;
        EditNotes      = Machine.Notes ?? string.Empty;
    }
}
