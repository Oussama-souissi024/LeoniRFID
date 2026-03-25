using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeoniRFID.Models;
using LeoniRFID.Services;
using System.Collections.ObjectModel;

namespace LeoniRFID.ViewModels;

[QueryProperty(nameof(MachineId), "machineId")]
public partial class MachineDetailViewModel : BaseViewModel
{
    private readonly DatabaseService _db;
    private readonly AuthService     _auth;

    public MachineDetailViewModel(DatabaseService db, AuthService auth)
    {
        _db   = db;
        _auth = auth;
        Title = "Détails Machine";
    }

    [ObservableProperty] private int      _machineId;
    [ObservableProperty] private Machine? _machine;
    [ObservableProperty] private bool     _isEditing  = false;
    [ObservableProperty] private bool     _isAdmin    = false;

    public ObservableCollection<ScanEvent> Events { get; } = [];

    // Editable fields
    [ObservableProperty] private string _editName       = string.Empty;
    [ObservableProperty] private string _editDepartment = string.Empty;
    [ObservableProperty] private string _editStatus     = string.Empty;
    [ObservableProperty] private string _editNotes      = string.Empty;

    partial void OnMachineIdChanged(int value) =>
        MainThread.BeginInvokeOnMainThread(async () => await LoadAsync());

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy  = true;
        IsAdmin = _auth.IsAdmin;
        try
        {
            Machine = await _db.GetMachineByIdAsync(MachineId);
            if (Machine is null) return;

            Title        = Machine.Name;
            EditName       = Machine.Name;
            EditDepartment = Machine.Department;
            EditStatus     = Machine.Status;
            EditNotes      = Machine.Notes ?? string.Empty;

            var events = await _db.GetEventsByMachineAsync(MachineId);
            Events.Clear();
            foreach (var e in events) Events.Add(e);
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
            Machine.Name       = EditName;
            Machine.Department = EditDepartment;
            Machine.Status     = EditStatus;
            Machine.Notes      = EditNotes;
            await _db.SaveMachineAsync(Machine);
            IsEditing = false;
            SetSuccess("Machine mise à jour.");
            await LoadAsync();
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        if (Machine is null) return;
        EditName       = Machine.Name;
        EditDepartment = Machine.Department;
        EditStatus     = Machine.Status;
        EditNotes      = Machine.Notes ?? string.Empty;
    }
}
