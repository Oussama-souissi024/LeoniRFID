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
    public bool IsMaintenance => _currentProfile?.Role == Constants.RoleMaintenance;
    public bool IsTechnician => _currentProfile?.Role == Constants.RoleTechnician;

    // Commentaire pédagogique :
    // - `CurrentProfile` expose le profil chargé après authentification.
    // - `IsAuthenticated` et `IsAdmin` sont des helpers utiles dans la vue pour afficher/masquer des éléments.

    public async Task<(bool Success, string Message)> LoginAsync(string email, string password)
    {
        try
        {
            var session = await _client.Auth.SignIn(email, password);
            if (session?.User is null)
                return (false, "Identifiants incorrects.");

            _currentProfile = await GetProfileAsync(session.User.Id);
            if (_currentProfile is null)
                return (false, "User profile not found.");

            if (!_currentProfile.IsActive)
                return (false, "Account disabled. Contact your administrator.");

            // Refresh local data cache for offline mode
            _ = DataCacheHelper.RefreshAsync(_client, Constants.SupabaseServiceRoleKey, Constants.SupabaseUrl);

            return (true, "Login successful!");
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}");
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

    public async Task<List<Machine>> GetMachinesByPlantAsync(string plant)
    {
        var response = await _client.From<Machine>()
            .Where(m => m.Plant == plant)
            .Get();
        return response.Models.ToList();
    }

    // Alias de compatibilité
    public Task<List<Machine>> GetMachinesByDepartmentAsync(string dept) => GetMachinesByPlantAsync(dept);

    public async Task<Machine?> GetMachineByTagReferenceAsync(string tagRef)
    {
        var response = await _client.From<Machine>()
            .Where(m => m.TagReference == tagRef)
            .Single();
        return response;
    }

    // Alias de compatibilité
    public Task<Machine?> GetMachineByTagIdAsync(string tagId) => GetMachineByTagReferenceAsync(tagId);

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

    public async Task SaveDepartmentAsync(Department department)
    {
        if (department.Id == 0)
            await _client.From<Department>().Insert(department);
        else
            await _client.From<Department>().Update(department);
    }

    public async Task DeleteDepartmentAsync(Department department)
    {
        await _client.From<Department>()
            .Where(d => d.Id == department.Id)
            .Delete();
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
                return (true, $"✅ Account created for {email}");
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                return (false, $"Supabase error: {errorBody}");
            }
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}");
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
                return (false, "User not found.");

            profile.Role = newRole;
            await _client.From<Profile>().Update(profile);
            return (true, $"Role updated: {newRole}");
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}");
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
                return (false, "User not found.");

            profile.IsActive = isActive;
            await _client.From<Profile>().Update(profile);
            return (true, isActive ? "✅ Account activated" : "❌ Account disabled");
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes a user account: removes the profile from DB and the auth user from Supabase.
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteUserAsync(string userId)
    {
        try
        {
            // 1. Delete profile from DB
            await _client.From<Profile>()
                .Where(p => p.Id == userId)
                .Delete();

            // 2. Delete auth user via Admin API
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("apikey", Constants.SupabaseServiceRoleKey);
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", Constants.SupabaseServiceRoleKey);

            var response = await httpClient.DeleteAsync(
                $"{Constants.SupabaseUrl}/auth/v1/admin/users/{userId}");

            if (response.IsSuccessStatusCode)
                return (true, "🗑️ Account deleted successfully.");
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                return (false, $"Profile removed but auth deletion failed: {errorBody}");
            }
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// 🎓 Pédagogie PFE : Bypass RLS (Row Level Security)
    /// Vérifie si un utilisateur doit encore définir son mot de passe (Zero-Knowledge).
    /// Pourquoi utiliser HttpClient et non l'API Supabase C# standard ?
    /// Parce que l'utilisateur n'est pas encore connecté (!). La politique de sécurité (RLS) 
    /// de la base de données bloque l'accès aux "Anonymes". On doit donc injecter manuellement
    /// la clé "ServiceRoleKey" (Privilèges Max) dans l'en-tête HTTP pour contourner ce blocage.
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

            // 🎓 Pédagogie PFE : Appel REST direct
            // Appel manuel de l'API /rest/v1/profiles pour lire *seulement* la colonne
            // must_change_password sans exposer tout le profil.
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
    /// 🎓 Pédagogie PFE : Principe du Zero-Knowledge Password
    /// Définit le mot de passe d'un technicien pour la toute première fois.
    /// Contrairement à une modification classique, c'est l'API d'Administration qui est 
    /// invoquée. Le mot de passe voyage de l'Application vers Supabase, où il est 
    /// immédiatement hashé de façon irréversible (bcrypt). Le booléen "must_change_password" 
    /// est ensuite verrouillé à "false" pour empêcher toute autre modification future par ce biais.
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
    // ══════════════════════════════════════════════════════════════════════
    //  MAINTENANCE SESSIONS (Workflow Maintenance)
    // ══════════════════════════════════════════════════════════════════════

    // 🎓 Pédagogie PFE : Démarrer une session de maintenance
    // Crée un enregistrement dans maintenance_sessions avec started_at = maintenant.
    // Le timer est calculé côté client en comparant DateTime.UtcNow - StartedAt.
    public async Task<MaintenanceSession> StartMaintenanceAsync(int machineId, string? technicianId)
    {
        var session = new MaintenanceSession
        {
            MachineId = machineId,
            TechnicianId = technicianId,
            StartedAt = DateTime.UtcNow
        };
        var response = await _client.From<MaintenanceSession>().Insert(session);
        return response.Models.First();
    }

    // 🎓 Pédagogie PFE : Terminer une session de maintenance
    // Renseigne ended_at et calcule automatiquement la durée en minutes.
    // ⚠️ Important : StartedAt revient de Supabase en heure locale, 
    // il faut le convertir en UTC avant de calculer la durée.
    public async Task EndMaintenanceAsync(MaintenanceSession session)
    {
        session.EndedAt = DateTime.UtcNow;
        var startUtc = session.StartedAt.ToUniversalTime();
        session.DurationMinutes = (session.EndedAt.Value - startUtc).TotalMinutes;
        await _client.From<MaintenanceSession>().Update(session);
    }

    // 🎓 Pédagogie PFE : Rechercher une maintenance en cours
    // Si ended_at IS NULL, la maintenance est encore active sur cette machine.
    public async Task<MaintenanceSession?> GetActiveMaintenanceAsync(int machineId)
    {
        try
        {
            var response = await _client.From<MaintenanceSession>()
                .Where(s => s.MachineId == machineId)
                .Filter("ended_at", Postgrest.Constants.Operator.Is, "null")
                .Single();
            return response;
        }
        catch { return null; }
    }

    // 🎓 Pédagogie PFE : Historique des maintenances (pour les rapports)
    public async Task<List<MaintenanceSession>> GetMaintenanceHistoryAsync(int? machineId = null)
    {
        Postgrest.Responses.ModeledResponse<MaintenanceSession> response;
        if (machineId.HasValue)
        {
            response = await _client.From<MaintenanceSession>()
                .Where(s => s.MachineId == machineId.Value)
                .Order("started_at", Postgrest.Constants.Ordering.Descending)
                .Get();
        }
        else
        {
            response = await _client.From<MaintenanceSession>()
                .Order("started_at", Postgrest.Constants.Ordering.Descending)
                .Get();
        }
        return response.Models.ToList();
    }
}
