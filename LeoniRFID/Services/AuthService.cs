using LeoniRFID.Models;
using Newtonsoft.Json;

namespace LeoniRFID.Services;

public class AuthService
{
    private readonly DatabaseService _db;
    private User? _currentUser;

    public AuthService(DatabaseService db) => _db = db;

    public User? CurrentUser => _currentUser;
    public bool IsAuthenticated => _currentUser is not null;
    public bool IsAdmin => _currentUser?.Role == Helpers.Constants.RoleAdmin;

    // ── Local Login ───────────────────────────────────────────────────────────
    public async Task<(bool Success, string Message)> LoginAsync(string email, string password)
    {
        try
        {
            var user = await _db.GetUserByEmailAsync(email.Trim());
            if (user is null)
                return (false, "Utilisateur introuvable.");

            var hash = DatabaseService.HashPassword(password);
            if (!string.Equals(user.PasswordHash, hash, StringComparison.OrdinalIgnoreCase))
                return (false, "Mot de passe incorrect.");

            if (!user.IsActive)
                return (false, "Compte désactivé. Contactez l'administrateur.");

            _currentUser = user;
            user.LastLoginAt = DateTime.UtcNow;
            await _db.SaveUserAsync(user);

            // Persist session
            var json = JsonConvert.SerializeObject(user);
            await SecureStorage.SetAsync(Helpers.Constants.CurrentUserKey, json);

            return (true, "Connexion réussie.");
        }
        catch (Exception ex)
        {
            return (false, $"Erreur : {ex.Message}");
        }
    }

    // ── Restore Session ───────────────────────────────────────────────────────
    public async Task<bool> TryRestoreSessionAsync()
    {
        try
        {
            var json = await SecureStorage.GetAsync(Helpers.Constants.CurrentUserKey);
            if (string.IsNullOrEmpty(json)) return false;
            _currentUser = JsonConvert.DeserializeObject<User>(json);
            return _currentUser is not null;
        }
        catch { return false; }
    }

    // ── Logout ────────────────────────────────────────────────────────────────
    public void Logout()
    {
        _currentUser = null;
        SecureStorage.Remove(Helpers.Constants.CurrentUserKey);
        SecureStorage.Remove(Helpers.Constants.SessionTokenKey);
    }

    // ── Google Sign-In (placeholder) ──────────────────────────────────────────
    /// <summary>
    /// Replace with actual Google Auth SDK integration.
    /// Requires: client_id from Google Cloud Console.
    /// </summary>
    public Task<(bool Success, string Message)> GoogleLoginAsync()
    {
        // TODO: Integrate Google.Apis.Auth or WebAuthenticator
        // var authUrl = $"https://accounts.google.com/o/oauth2/auth?client_id={Constants.GoogleClientId}&...";
        // var result = await WebAuthenticator.Default.AuthenticateAsync(...);
        return Task.FromResult((false, "Google Auth : configurez votre client_id OAuth 2.0 dans Constants.cs"));
    }

    // ── Change Password ───────────────────────────────────────────────────────
    public async Task<(bool, string)> ChangePasswordAsync(string currentPwd, string newPwd)
    {
        if (_currentUser is null) return (false, "Non authentifié.");
        var hash = DatabaseService.HashPassword(currentPwd);
        if (!string.Equals(_currentUser.PasswordHash, hash, StringComparison.OrdinalIgnoreCase))
            return (false, "Mot de passe actuel incorrect.");

        _currentUser.PasswordHash = DatabaseService.HashPassword(newPwd);
        await _db.SaveUserAsync(_currentUser);
        return (true, "Mot de passe modifié avec succès.");
    }
}
