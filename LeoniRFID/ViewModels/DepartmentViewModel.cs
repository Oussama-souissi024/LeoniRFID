using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeoniRFID.Models;
using LeoniRFID.Services;
using System.Collections.ObjectModel;

namespace LeoniRFID.ViewModels;

// 🎓 Pédagogie PFE : ViewModel de gestion des départements
// Ce ViewModel gère le CRUD complet des départements (Plants/Sites LEONI).
// Accessible uniquement aux administrateurs via le contrôle RBAC dans AppShell.
public partial class DepartmentViewModel : BaseViewModel
{
    private readonly SupabaseService _supabase;

    public DepartmentViewModel(SupabaseService supabase)
    {
        _supabase = supabase;
        Title = "Departments";
    }

    // ── Collections ──────────────────────────────────────────────────────
    public ObservableCollection<Department> Departments { get; } = [];

    // ── Formulaire ───────────────────────────────────────────────────────
    [ObservableProperty] private bool _isFormVisible;
    [ObservableProperty] private string _formTitle = "New Department";
    [ObservableProperty] private string _deptCode = string.Empty;
    [ObservableProperty] private string _deptName = string.Empty;
    [ObservableProperty] private string _deptDescription = string.Empty;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string? _successMessage;

    private Department? _editingDepartment;

    // ══════════════════════════════════════════════════════════════════════
    //  CHARGEMENT
    // ══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var depts = await _supabase.GetDepartmentsAsync();
            Departments.Clear();
            foreach (var d in depts.OrderBy(d => d.Code))
                Departments.Add(d);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  FORMULAIRE : AFFICHER / MASQUER
    // ══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private void ToggleForm()
    {
        if (IsFormVisible)
        {
            // Fermer le formulaire
            ResetForm();
        }
        else
        {
            // Ouvrir en mode création
            _editingDepartment = null;
            FormTitle = "New Department";
            DeptCode = string.Empty;
            DeptName = string.Empty;
            DeptDescription = string.Empty;
            IsFormVisible = true;
        }
        ErrorMessage = null;
        SuccessMessage = null;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  SAUVEGARDER (Créer ou Modifier)
    // ══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;

        if (string.IsNullOrWhiteSpace(DeptCode) || string.IsNullOrWhiteSpace(DeptName))
        {
            ErrorMessage = "Code and name are required.";
            return;
        }

        IsBusy = true;
        try
        {
            if (_editingDepartment is not null)
            {
                // Mode édition
                _editingDepartment.Code = DeptCode.Trim().ToUpper();
                _editingDepartment.Name = DeptName.Trim();
                _editingDepartment.Description = string.IsNullOrWhiteSpace(DeptDescription) ? null : DeptDescription.Trim();
                await _supabase.SaveDepartmentAsync(_editingDepartment);
                SuccessMessage = $"✅ Department « {_editingDepartment.Code} » updated.";
            }
            else
            {
                // Mode création
                var dept = new Department
                {
                    Code = DeptCode.Trim().ToUpper(),
                    Name = DeptName.Trim(),
                    Description = string.IsNullOrWhiteSpace(DeptDescription) ? null : DeptDescription.Trim()
                };
                await _supabase.SaveDepartmentAsync(dept);
                SuccessMessage = $"✅ Department « {dept.Code} » successfully created.";
            }

            ResetForm();
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  MODIFIER
    // ══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private void EditDepartment(Department dept)
    {
        if (dept is null) return;
        _editingDepartment = dept;
        FormTitle = $"Edit « {dept.Code} »";
        DeptCode = dept.Code;
        DeptName = dept.Name;
        DeptDescription = dept.Description ?? string.Empty;
        IsFormVisible = true;
        ErrorMessage = null;
        SuccessMessage = null;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  SUPPRIMER
    // ══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private async Task DeleteDepartmentAsync(Department dept)
    {
        if (dept is null) return;

        bool confirm = await Shell.Current.DisplayAlert(
            "Delete",
            $"Delete the department « {dept.Code} — {dept.Name} »?\n\nThis action is irreversible.",
            "Delete", "Cancel");

        if (!confirm) return;

        IsBusy = true;
        try
        {
            await _supabase.DeleteDepartmentAsync(dept);
            SuccessMessage = $"🗑️ Department « {dept.Code} » deleted.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════════════════════════

    private void ResetForm()
    {
        _editingDepartment = null;
        IsFormVisible = false;
        DeptCode = string.Empty;
        DeptName = string.Empty;
        DeptDescription = string.Empty;
        FormTitle = "New Department";
    }
}
