using LeoniRFID.Helpers;
using LeoniRFID.Models;

namespace LeoniRFID.Services;

public class SupabaseService
{
    private readonly Supabase.Client _client;
    private Profile? _currentProfile;

    // Commentaire pédagogique :
    // - `SupabaseService` encapsule l'accès au backend Supabase (auth, CRUD).
    // - Toujours garder le code d'accès réseau séparé des ViewModels pour respecter le principe de séparation des responsabilités.

    public SupabaseService()
    {
        var options = new Supabase.SupabaseOptions
        {
            AutoConnectRealtime = false
        };
        _client = new Supabase.Client(
            Constants.SupabaseUrl,
            Constants.SupabaseAnonKey,
            options
        );
    }

    public async Task InitializeAsync()
    {
        await _client.InitializeAsync();
    }

    // ══════════════════════════════════════════════════════════════════════
    //  AUTHENTIFICATION
    // ══════════════════════════════════════════════════════════════════════

    public Profile? CurrentProfile => _currentProfile;
    public bool IsAuthenticated => _client.Auth.CurrentUser is not null;
    public bool IsAdmin => _currentProfile?.Role == Constants.RoleAdmin;

    public async Task<(bool Success, string Message)> LoginAsync(string email, string password)
    {
        try
        {
            var session = await _client.Auth.SignIn(email, password);
            if (session?.User is null)
                return (false, "Identifiants incorrects.");

            _currentProfile = await GetProfileAsync(session.User.Id);
            if (_currentProfile is null)
                return (false, "Profil utilisateur introuvable.");

            if (!_currentProfile.IsActive)
                return (false, "Compte désactivé. Contactez l'administrateur.");

            return (true, "Connexion réussie !");
        }
        catch (Exception ex)
        {
            return (false, $"Erreur : {ex.Message}");
        }
    }

    public async Task LogoutAsync()
    {
        await _client.Auth.SignOut();
        _currentProfile = null;
    }

    public async Task<bool> TryRestoreSessionAsync()
    {
        try
        {
            var session = _client.Auth.CurrentSession;
            if (session?.User is null) return false;

            _currentProfile = await GetProfileAsync(session.User.Id);
            return _currentProfile is not null;
        }
        catch { return false; }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  PROFILES
    // ══════════════════════════════════════════════════════════════════════

    private async Task<Profile?> GetProfileAsync(string userId)
    {
        var response = await _client.From<Profile>()
            .Where(p => p.Id == userId)
            .Single();
        return response;
    }

    public async Task<List<Profile>> GetAllProfilesAsync()
    {
        var response = await _client.From<Profile>().Get();
        return response.Models.ToList();
    }

    // ══════════════════════════════════════════════════════════════════════
    //  MACHINES
    // ══════════════════════════════════════════════════════════════════════

    public async Task<List<Machine>> GetAllMachinesAsync()
    {
        var response = await _client.From<Machine>().Get();
        return response.Models.ToList();
    }

    public async Task<List<Machine>> GetMachinesByDepartmentAsync(string dept)
    {
        var response = await _client.From<Machine>()
            .Where(m => m.Department == dept)
            .Get();
        return response.Models.ToList();
    }

    public async Task<Machine?> GetMachineByTagIdAsync(string tagId)
    {
        var response = await _client.From<Machine>()
            .Where(m => m.TagId == tagId)
            .Single();
        return response;
    }

    public async Task<Machine?> GetMachineByIdAsync(int id)
    {
        var response = await _client.From<Machine>()
            .Where(m => m.Id == id)
            .Single();
        return response;
    }

    public async Task SaveMachineAsync(Machine machine)
    {
        machine.LastUpdated = DateTime.UtcNow;
        if (machine.Id == 0)
            await _client.From<Machine>().Insert(machine);
        else
            await _client.From<Machine>().Update(machine);
    }

    public async Task DeleteMachineAsync(Machine machine)
    {
        await _client.From<Machine>()
            .Where(m => m.Id == machine.Id)
            .Delete();
    }

    public async Task BulkInsertMachinesAsync(List<Machine> machines)
    {
        foreach (var m in machines)
        {
            m.LastUpdated = DateTime.UtcNow;
        }
        await _client.From<Machine>().Insert(machines);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  SCAN EVENTS
    // ══════════════════════════════════════════════════════════════════════

    public async Task SaveScanEventAsync(ScanEvent scanEvent)
    {
        await _client.From<ScanEvent>().Insert(scanEvent);
    }

    public async Task<List<ScanEvent>> GetRecentEventsAsync(int count = 20)
    {
        var response = await _client.From<ScanEvent>()
            .Order("timestamp", Postgrest.Constants.Ordering.Descending)
            .Limit(count)
            .Get();
        return response.Models.ToList();
    }

    public async Task<List<ScanEvent>> GetEventsByMachineAsync(int machineId)
    {
        var response = await _client.From<ScanEvent>()
            .Where(e => e.MachineId == machineId)
            .Order("timestamp", Postgrest.Constants.Ordering.Descending)
            .Get();
        return response.Models.ToList();
    }

    // ══════════════════════════════════════════════════════════════════════
    //  DEPARTMENTS
    // ══════════════════════════════════════════════════════════════════════

    public async Task<List<Department>> GetDepartmentsAsync()
    {
        var response = await _client.From<Department>().Get();
        return response.Models.ToList();
    }

    // ══════════════════════════════════════════════════════════════════════
    //  GESTION DES UTILISATEURS (Admin Only)
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Crée un nouveau compte utilisateur via l'API Admin de Supabase.
    /// Génère un mot de passe temporaire aléatoire que personne ne connaît.
    /// </summary>
    public async Task<(bool Success, string Message)> CreateUserAsync(
        string email, string fullName, string role)
    {
        try
        {
            // Mot de passe temporaire aléatoire (personne ne le connaîtra)
            var tempPassword = Guid.NewGuid().ToString("N") + "Ab1!";

            // Appeler l'API REST Admin de Supabase pour créer l'utilisateur
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("apikey", Constants.SupabaseServiceRoleKey);
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", Constants.SupabaseServiceRoleKey);

            var payload = new
            {
                email = email,
                password = tempPassword,
                email_confirm = true,
                user_metadata = new { full_name = fullName, role = role }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(
                $"{Constants.SupabaseUrl}/auth/v1/admin/users", content);

            if (response.IsSuccessStatusCode)
            {
                return (true, $"✅ Compte créé pour {email}");
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                return (false, $"Erreur Supabase : {errorBody}");
            }
        }
        catch (Exception ex)
        {
            return (false, $"Erreur : {ex.Message}");
        }
    }

    /// <summary>
    /// Récupère tous les profils utilisateurs (pour la page Admin).
    /// </summary>
    public async Task<List<Profile>> GetAllUsersAsync()
    {
        var response = await _client.From<Profile>()
            .Order("created_at", Postgrest.Constants.Ordering.Descending)
            .Get();
        return response.Models.ToList();
    }

    /// <summary>
    /// Met à jour le rôle d'un utilisateur (Technician ↔ Admin).
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateUserRoleAsync(
        string userId, string newRole)
    {
        try
        {
            var profile = await _client.From<Profile>()
                .Where(p => p.Id == userId)
                .Single();

            if (profile is null)
                return (false, "Utilisateur introuvable.");

            profile.Role = newRole;
            await _client.From<Profile>().Update(profile);
            return (true, $"Rôle mis à jour : {newRole}");
        }
        catch (Exception ex)
        {
            return (false, $"Erreur : {ex.Message}");
        }
    }

    /// <summary>
    /// Active ou désactive un compte utilisateur.
    /// </summary>
    public async Task<(bool Success, string Message)> ToggleUserActiveAsync(
        string userId, bool isActive)
    {
        try
        {
            var profile = await _client.From<Profile>()
                .Where(p => p.Id == userId)
                .Single();

            if (profile is null)
                return (false, "Utilisateur introuvable.");

            profile.IsActive = isActive;
            await _client.From<Profile>().Update(profile);
            return (true, isActive ? "✅ Compte activé" : "❌ Compte désactivé");
        }
        catch (Exception ex)
        {
            return (false, $"Erreur : {ex.Message}");
        }
    }

    /// <summary>
    /// Vérifie si un utilisateur doit encore définir son mot de passe.
    /// Retourne true si must_change_password == true (case activée).
    /// Retourne false si le mot de passe a déjà été défini (case grisée).
    /// Retourne null si l'email n'existe pas.
    /// </summary>
    public async Task<bool?> CheckFirstLoginStatusAsync(string email)
    {
        try
        {
            // Chercher l'utilisateur dans auth.users via l'API Admin
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("apikey", Constants.SupabaseServiceRoleKey);
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", Constants.SupabaseServiceRoleKey);

            var listResponse = await httpClient.GetAsync(
                $"{Constants.SupabaseUrl}/auth/v1/admin/users");

            if (!listResponse.IsSuccessStatusCode) return null;

            var listJson = await listResponse.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(listJson);

            string? userId = null;
            if (doc.RootElement.TryGetProperty("users", out var usersArray))
            {
                foreach (var user in usersArray.EnumerateArray())
                {
                    var userEmail = user.GetProperty("email").GetString();
                    if (string.Equals(userEmail, email, StringComparison.OrdinalIgnoreCase))
                    {
                        userId = user.GetProperty("id").GetString();
                        break;
                    }
                }
            }

            if (userId is null) return null;

            // Lire le profil via l'API REST avec les droits Admin (bypass RLS)
            var profileResponse = await httpClient.GetAsync(
                $"{Constants.SupabaseUrl}/rest/v1/profiles?id=eq.{userId}&select=must_change_password");

            if (!profileResponse.IsSuccessStatusCode) return null;

            var profileJson = await profileResponse.Content.ReadAsStringAsync();
            using var profileDoc = System.Text.Json.JsonDocument.Parse(profileJson);

            if (profileDoc.RootElement.GetArrayLength() > 0)
            {
                var p = profileDoc.RootElement[0];
                if (p.TryGetProperty("must_change_password", out var pwdProp))
                    return pwdProp.GetBoolean();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Définit le mot de passe d'un technicien pour la première fois.
    /// Utilise l'API Admin (service_role) pour :
    /// 1. Trouver l'utilisateur par email
    /// 2. Mettre à jour son mot de passe (hashé par Supabase)
    /// 3. Passer must_change_password à false
    /// </summary>
    public async Task<(bool Success, string Message)> SetFirstPasswordViaAdminAsync(
        string email, string newPassword)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("apikey", Constants.SupabaseServiceRoleKey);
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", Constants.SupabaseServiceRoleKey);

            // 1. Chercher l'utilisateur par email via l'API Admin
            var listResponse = await httpClient.GetAsync(
                $"{Constants.SupabaseUrl}/auth/v1/admin/users");

            if (!listResponse.IsSuccessStatusCode)
                return (false, "Impossible de vérifier les utilisateurs.");

            var listJson = await listResponse.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(listJson);

            // Chercher l'utilisateur dans la liste
            string? userId = null;
            if (doc.RootElement.TryGetProperty("users", out var usersArray))
            {
                foreach (var user in usersArray.EnumerateArray())
                {
                    var userEmail = user.GetProperty("email").GetString();
                    if (string.Equals(userEmail, email, StringComparison.OrdinalIgnoreCase))
                    {
                        userId = user.GetProperty("id").GetString();
                        break;
                    }
                }
            }

            if (userId is null)
                return (false, "Aucun compte trouvé avec cet email.");

            // 2. Vérifier que must_change_password == true via l'API REST avec droits Admin
            var profileResponse = await httpClient.GetAsync(
                $"{Constants.SupabaseUrl}/rest/v1/profiles?id=eq.{userId}&select=must_change_password");

            if (!profileResponse.IsSuccessStatusCode)
                return (false, "Erreur réseau lors de la lecture du profil.");

            var profileJson = await profileResponse.Content.ReadAsStringAsync();
            using var profileDoc = System.Text.Json.JsonDocument.Parse(profileJson);

            if (profileDoc.RootElement.GetArrayLength() == 0)
                return (false, "Profil introuvable.");

            bool mustChange = profileDoc.RootElement[0].GetProperty("must_change_password").GetBoolean();

            if (!mustChange)
                return (false, "⚠️ Vous avez déjà défini votre mot de passe. Utilisez 'SE CONNECTER'.");

            // 3. Mettre à jour le mot de passe via l'API Admin
            var payload = new { password = newPassword };
            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var updateResponse = await httpClient.PutAsync(
                $"{Constants.SupabaseUrl}/auth/v1/admin/users/{userId}", content);

            if (!updateResponse.IsSuccessStatusCode)
            {
                var errorBody = await updateResponse.Content.ReadAsStringAsync();
                return (false, $"Erreur : {errorBody}");
            }

            // 4. Marquer must_change_password = false via l'API REST avec droits Admin
            var patchPayload = new { must_change_password = false };
            var patchJson = System.Text.Json.JsonSerializer.Serialize(patchPayload);
            var patchContent = new StringContent(patchJson, System.Text.Encoding.UTF8, "application/json");

            var patchResponse = await httpClient.PatchAsync(
                $"{Constants.SupabaseUrl}/rest/v1/profiles?id=eq.{userId}", patchContent);

            if (!patchResponse.IsSuccessStatusCode)
            {
                var errorBody = await patchResponse.Content.ReadAsStringAsync();
                return (false, $"Profil non mis à jour : {errorBody}");
            }

            return (true, "✅ Mot de passe défini avec succès ! Vous pouvez maintenant vous connecter.");
        }
        catch (Exception ex)
        {
            return (false, $"Erreur : {ex.Message}");
        }
    }

    /// <summary>
    /// Change le mot de passe de l'utilisateur actuellement connecté.
    /// </summary>
    public async Task<(bool Success, string Message)> ChangePasswordAsync(string newPassword)
    {
        try
        {
            var attrs = new Supabase.Gotrue.UserAttributes { Password = newPassword };
            await _client.Auth.Update(attrs);

            // Marquer must_change_password = false dans le profil
            if (_currentProfile != null)
            {
                _currentProfile.MustChangePassword = false;
                await _client.From<Profile>().Update(_currentProfile);
            }

            return (true, "✅ Mot de passe mis à jour avec succès !");
        }
        catch (Exception ex)
        {
            return (false, $"Erreur : {ex.Message}");
        }
    }
}
