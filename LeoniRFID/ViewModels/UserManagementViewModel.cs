using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeoniRFID.Models;
using LeoniRFID.Services;
using System.Collections.ObjectModel;

namespace LeoniRFID.ViewModels;

public partial class UserManagementViewModel : BaseViewModel
{
    private readonly SupabaseService _supabase;

    public UserManagementViewModel(SupabaseService supabase)
    {
        _supabase = supabase;
        Title = "Gestion des utilisateurs";
    }

    // ── Liste des utilisateurs ──
    public ObservableCollection<Profile> Users { get; } = new();

    // ── Formulaire de création ──
    [ObservableProperty] private string _newUserName = string.Empty;
    [ObservableProperty] private string _newUserEmail = string.Empty;
    [ObservableProperty] private string _selectedRole = "Technician";
    [ObservableProperty] private bool _isFormVisible = false;

    // ── Rôles disponibles ──
    public List<string> AvailableRoles { get; } = new() { "Technician", "Admin" };

    [RelayCommand]
    private async Task LoadUsersAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        ClearMessages();

        try
        {
            var users = await _supabase.GetAllUsersAsync();
            Users.Clear();
            foreach (var user in users)
                Users.Add(user);
        }
        catch (Exception ex)
        {
            SetError($"Erreur chargement : {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ToggleForm()
    {
        IsFormVisible = !IsFormVisible;
        if (!IsFormVisible)
        {
            NewUserName = string.Empty;
            NewUserEmail = string.Empty;
            SelectedRole = "Technician";
        }
    }

    [RelayCommand]
    private async Task CreateUserAsync()
    {
        if (IsBusy) return;
        ClearMessages();

        if (string.IsNullOrWhiteSpace(NewUserName))
        { SetError("Le nom complet est requis."); return; }

        if (string.IsNullOrWhiteSpace(NewUserEmail) || !NewUserEmail.Contains('@'))
        { SetError("Un email valide est requis."); return; }

        IsBusy = true;
        try
        {
            var (success, message) = await _supabase.CreateUserAsync(
                NewUserEmail.Trim().ToLower(),
                NewUserName.Trim(),
                SelectedRole);

            if (success)
            {
                SetSuccess(message);
                NewUserName = string.Empty;
                NewUserEmail = string.Empty;
                SelectedRole = "Technician";
                IsFormVisible = false;
                await LoadUsersAsync();
            }
            else
            {
                SetError(message);
            }
        }
        catch (Exception ex)
        {
            SetError($"Erreur : {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ToggleUserStatusAsync(Profile user)
    {
        if (IsBusy) return;
        IsBusy = true;

        var (success, message) = await _supabase.ToggleUserActiveAsync(
            user.Id, !user.IsActive);

        if (success)
            SetSuccess(message);
        else
            SetError(message);

        await LoadUsersAsync();
        IsBusy = false;
    }

    [RelayCommand]
    private async Task ChangeRoleAsync(Profile user)
    {
        var newRole = user.Role == "Admin" ? "Technician" : "Admin";
        
        bool confirm = await Shell.Current.DisplayAlert(
            "Changer le rôle",
            $"Passer {user.FullName} en « {newRole} » ?",
            "Oui", "Non");

        if (!confirm) return;

        IsBusy = true;
        var (success, message) = await _supabase.UpdateUserRoleAsync(user.Id, newRole);

        if (success)
            SetSuccess(message);
        else
            SetError(message);

        await LoadUsersAsync();
        IsBusy = false;
    }
}
