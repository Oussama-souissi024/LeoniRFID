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
        Title = "User Management";
    }

    // ── Liste des utilisateurs ──
    // 🎓 Pédagogie PFE : ObservableCollection vs List
    // Une `List<Profile>` classique ne prévient pas l'interface quand on y ajoute un élément.
    // Une `ObservableCollection` demande au XAML de redessiner la liste automatiquement 
    // à chaque ajout (`Add`) ou suppression (`Remove`), idéal pour une ListView ou CollectionView.
    public ObservableCollection<Profile> Users { get; } = new();

    // ── Formulaire de création ──
    [ObservableProperty] private string _newUserName = string.Empty;
    [ObservableProperty] private string _newUserEmail = string.Empty;
    [ObservableProperty] private string _selectedRole = "Technician";
    [ObservableProperty] private int _selectedRoleIndex = 0;
    [ObservableProperty] private bool _isFormVisible = false;

    // ── Rôles disponibles ──
    public List<string> AvailableRoles { get; } = new() { "Technician", "Maintenance", "Admin" };
    public List<string> AvailableRolesDisplay { get; } = new() { "👷 Technician", "🔧 Maintenance Agent", "👑 Administrator" };

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
            SetError($"Loading error: {ex.Message}");
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
            SelectedRoleIndex = 0;
        }
    }

    [RelayCommand]
    private async Task CreateUserAsync()
    {
        if (IsBusy) return;
        ClearMessages();

        if (string.IsNullOrWhiteSpace(NewUserName))
        { SetError("Full name is required."); return; }

        if (string.IsNullOrWhiteSpace(NewUserEmail) || !NewUserEmail.Contains('@'))
        { SetError("Valid email is required."); return; }

        IsBusy = true;
        try
        {
            // Mapper l'index du Picker au rôle technique
            var role = AvailableRoles[SelectedRoleIndex];

            var (success, message) = await _supabase.CreateUserAsync(
                NewUserEmail.Trim().ToLower(),
                NewUserName.Trim(),
                role);

            if (success)
            {
                SetSuccess(message);
                NewUserName = string.Empty;
                NewUserEmail = string.Empty;
                SelectedRoleIndex = 0;
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
            SetError($"Error: {ex.Message}");
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

        string newStatus = user.IsActive ? "Disable" : "Enable";
        bool confirm = await Shell.Current.DisplayAlert(
            "Change Status",
            $"{newStatus} the account of {user.FullName}?",
            newStatus, "Cancel");

        if (!confirm) return;

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
        // Proposer les 3 rôles possibles
        string action = await Shell.Current.DisplayActionSheet(
            $"Change role for {user.FullName}",
            "Cancel", null,
            "👷 Technician", "🔧 Maintenance Agent", "👑 Administrator");

        string? newRole = action switch
        {
            "👷 Technician"        => "Technician",
            "🔧 Maintenance Agent" => "Maintenance",
            "👑 Administrator"     => "Admin",
            _ => null
        };

        if (newRole is null || newRole == user.Role) return;

        IsBusy = true;
        var (success, message) = await _supabase.UpdateUserRoleAsync(user.Id, newRole);

        if (success)
            SetSuccess(message);
        else
            SetError(message);

        await LoadUsersAsync();
        IsBusy = false;
    }

    [RelayCommand]
    private async Task DeleteUserAsync(Profile user)
    {
        if (IsBusy) return;

        bool confirm = await Shell.Current.DisplayAlert(
            "⚠️ Delete Account",
            $"Permanently delete the account of {user.FullName}?\n\nThis action is irreversible.",
            "Delete", "Cancel");

        if (!confirm) return;

        IsBusy = true;
        try
        {
            var (success, message) = await _supabase.DeleteUserAsync(user.Id);

            if (success)
            {
                SetSuccess(message);
                Users.Remove(user);
            }
            else
                SetError(message);
        }
        catch (Exception ex)
        {
            SetError($"Error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
