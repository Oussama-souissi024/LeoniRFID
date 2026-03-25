using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeoniRFID.Services;
using LeoniRFID.Views;

namespace LeoniRFID.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly AuthService _auth;
    private readonly DatabaseService _db;

    public LoginViewModel(AuthService auth, DatabaseService db)
    {
        _auth = auth;
        _db   = db;
        Title = "Connexion LEONI";
    }

    [ObservableProperty] private string _email    = string.Empty;
    [ObservableProperty] private string _password  = string.Empty;
    [ObservableProperty] private bool   _isPasswordVisible = false;

    public string PasswordToggleIcon => IsPasswordVisible ? "eye_off.png" : "eye.png";

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
            await _db.InitAsync();
            var (success, message) = await _auth.LoginAsync(Email.Trim(), Password);

            if (success)
            {
                SetSuccess("Connexion réussie !");
                await Task.Delay(300); // brief visual feedback
                await Shell.Current.GoToAsync("//dashboard");
            }
            else
            {
                SetError(message);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoogleLoginAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var (success, message) = await _auth.GoogleLoginAsync();
            if (success)
                await Shell.Current.GoToAsync("//dashboard");
            else
                SetError(message);
        }
        finally { IsBusy = false; }
    }
}
