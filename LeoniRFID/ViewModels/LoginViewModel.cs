// Commentaire pédagogique :
// - `LoginViewModel` contient la logique d'authentification et expose les commandes/états utilisés par la vue `LoginPage`.
// - Les ViewModels doivent rester sans dépendances UI pour faciliter le test unitaire.
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeoniRFID.Services;
using LeoniRFID.Views;

namespace LeoniRFID.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly SupabaseService _supabase;

    public LoginViewModel(SupabaseService supabase)
    {
        _supabase = supabase;
        Title = "Connexion LEONI";
    }

    [ObservableProperty] private string _email    = string.Empty;
    [ObservableProperty] private string _password  = string.Empty;
    [ObservableProperty] private bool   _isPasswordVisible = false;
    [ObservableProperty] private bool   _isFirstLogin     = false;
    [ObservableProperty] private string _newPassword       = string.Empty;
    [ObservableProperty] private string _confirmPassword   = string.Empty;

    // ── Nouvelles propriétés ──
    [ObservableProperty] private bool _canUseFirstLogin = true;
    [ObservableProperty] private string _firstLoginHint = string.Empty;
    [ObservableProperty] private Color _firstLoginTextColor = Colors.White;

    public string PasswordToggleIcon => IsPasswordVisible ? "eye_off.png" : "eye.png";

    // ── Vérification automatique quand l'email change ──
    partial void OnEmailChanged(string value)
    {
        // Réinitialiser quand l'email change
        if (!string.IsNullOrWhiteSpace(value) && value.Contains('@'))
        {
            _ = CheckFirstLoginAsync(value.Trim());
        }
    }

    private async Task CheckFirstLoginAsync(string email)
    {
        await _supabase.InitializeAsync();
        var status = await _supabase.CheckFirstLoginStatusAsync(email);

        if (status == true)
        {
            CanUseFirstLogin = true;
            FirstLoginTextColor = Colors.White;
            FirstLoginHint = string.Empty;
        }
        else if (status == false)
        {
            CanUseFirstLogin = false;
            IsFirstLogin = false;
            FirstLoginTextColor = Color.FromArgb("#666666");
            FirstLoginHint = "🔒 Mot de passe déjà défini.";
        }
        else
        {
            // Email pas encore trouvé ou erreur réseau → garder la case activée
            CanUseFirstLogin = true;
            FirstLoginTextColor = Colors.White;
            FirstLoginHint = string.Empty;
        }
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordVisible = !IsPasswordVisible;
        OnPropertyChanged(nameof(PasswordToggleIcon));
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy) return;
        ClearMessages();

        if (string.IsNullOrWhiteSpace(Email))    { SetError("Veuillez saisir votre email."); return; }
        if (string.IsNullOrWhiteSpace(Password)) { SetError("Veuillez saisir votre mot de passe."); return; }

        IsBusy = true;
        try
        {
            await _supabase.InitializeAsync();
            var (success, message) = await _supabase.LoginAsync(Email.Trim(), Password);

            if (success)
            {
                SetSuccess("Connexion réussie !");
                await Task.Delay(300);
                await Shell.Current.GoToAsync("//dashboard");
            }
            else
            {
                SetError(message);
            }
        }
        catch (Exception ex)
        {
            // Log and show a friendly error instead of crashing the app
            System.Diagnostics.Debug.WriteLine($"[CRASH] LoginAsync: {ex}");
            SetError($"Une erreur est survenue lors de la connexion : {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ToggleFirstLogin()
    {
        IsFirstLogin = !IsFirstLogin;
    }

    [RelayCommand]
    private async Task SetFirstPasswordAsync()
    {
        if (IsBusy) return;
        ClearMessages();

        if (string.IsNullOrWhiteSpace(Email))
        { SetError("Veuillez saisir votre email."); return; }

        if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 6)
        { SetError("Le mot de passe doit contenir au moins 6 caractères."); return; }

        if (NewPassword != ConfirmPassword)
        { SetError("Les mots de passe ne correspondent pas."); return; }

        IsBusy = true;
        try
        {
            await _supabase.InitializeAsync();
            var (success, message) = await _supabase.SetFirstPasswordViaAdminAsync(
                Email.Trim(), NewPassword);

            if (success)
            {
                SetSuccess(message);
                IsFirstLogin = false;
                CanUseFirstLogin = false;
                FirstLoginHint = "🔒 Mot de passe déjà défini.";
                NewPassword = string.Empty;
                ConfirmPassword = string.Empty;
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
}
