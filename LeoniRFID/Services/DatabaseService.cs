using LeoniRFID.Models;
using SQLite;

namespace LeoniRFID.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection? _db;

    public async Task InitAsync()
    {
        if (_db is not null) return;

        _db = new SQLiteAsyncConnection(Helpers.Constants.DatabasePath,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

        await _db.CreateTableAsync<User>();
        await _db.CreateTableAsync<Department>();
        await _db.CreateTableAsync<Machine>();
        await _db.CreateTableAsync<ScanEvent>();

        await SeedDefaultDataAsync();
    }

    // ── Seed ─────────────────────────────────────────────────────────────────
    private async Task SeedDefaultDataAsync()
    {
        // Departments
        var deptCount = await _db!.Table<Department>().CountAsync();
        if (deptCount == 0)
        {
            await _db.InsertAllAsync(Department.DefaultDepartments);
        }

        // Default users
        var userCount = await _db.Table<User>().CountAsync();
        if (userCount == 0)
        {
            await _db.InsertAllAsync(new[]
            {
                new User
                {
                    FullName = "Administrateur LEONI",
                    Email    = Helpers.Constants.TestAdminEmail,
                    PasswordHash = HashPassword(Helpers.Constants.TestAdminPassword),
                    Role     = Helpers.Constants.RoleAdmin,
                    IsActive = true
                },
                new User
                {
                    FullName = "Technicien Atelier",
                    Email    = Helpers.Constants.TestTechEmail,
                    PasswordHash = HashPassword(Helpers.Constants.TestTechPassword),
                    Role     = Helpers.Constants.RoleTechnician,
                    IsActive = true
                }
            });
        }
    }

    // ── Users ─────────────────────────────────────────────────────────────────
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        await InitAsync();
        return await _db!.Table<User>()
            .Where(u => u.Email.ToLower() == email.ToLower() && u.IsActive)
            .FirstOrDefaultAsync();
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        await InitAsync();
        return await _db!.Table<User>().ToListAsync();
    }

    public async Task<int> SaveUserAsync(User user)
    {
        await InitAsync();
        if (user.Id == 0) return await _db!.InsertAsync(user);
        return await _db!.UpdateAsync(user);
    }

    // ── Machines ──────────────────────────────────────────────────────────────
    public async Task<List<Machine>> GetAllMachinesAsync()
    {
        await InitAsync();
        return await _db!.Table<Machine>().ToListAsync();
    }

    public async Task<List<Machine>> GetMachinesByDepartmentAsync(string dept)
    {
        await InitAsync();
        return await _db!.Table<Machine>().Where(m => m.Department == dept).ToListAsync();
    }

    public async Task<Machine?> GetMachineByTagIdAsync(string tagId)
    {
        await InitAsync();
        return await _db!.Table<Machine>()
            .Where(m => m.TagId == tagId)
            .FirstOrDefaultAsync();
    }

    public async Task<Machine?> GetMachineByIdAsync(int id)
    {
        await InitAsync();
        return await _db!.FindAsync<Machine>(id);
    }

    public async Task<int> SaveMachineAsync(Machine machine)
    {
        await InitAsync();
        machine.LastUpdated = DateTime.UtcNow;
        machine.IsSynced = false;
        if (machine.Id == 0) return await _db!.InsertAsync(machine);
        return await _db!.UpdateAsync(machine);
    }

    public async Task<int> DeleteMachineAsync(Machine machine)
    {
        await InitAsync();
        return await _db!.DeleteAsync(machine);
    }

    public async Task<int> GetMachineCountByStatusAsync(string status)
    {
        await InitAsync();
        return await _db!.Table<Machine>().Where(m => m.Status == status).CountAsync();
    }

    public async Task BulkInsertMachinesAsync(List<Machine> machines)
    {
        await InitAsync();
        await _db!.RunInTransactionAsync(conn =>
        {
            foreach (var m in machines)
            {
                m.LastUpdated = DateTime.UtcNow;
                conn.Insert(m);
            }
        });
    }

    // ── Scan Events ───────────────────────────────────────────────────────────
    public async Task<int> SaveScanEventAsync(ScanEvent scanEvent)
    {
        await InitAsync();
        return await _db!.InsertAsync(scanEvent);
    }

    public async Task<List<ScanEvent>> GetRecentEventsAsync(int count = 20)
    {
        await InitAsync();
        return await _db!.Table<ScanEvent>()
            .OrderByDescending(e => e.Timestamp)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<ScanEvent>> GetEventsByMachineAsync(int machineId)
    {
        await InitAsync();
        return await _db!.Table<ScanEvent>()
            .Where(e => e.MachineId == machineId)
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync();
    }

    public async Task<List<ScanEvent>> GetUnsyncedEventsAsync()
    {
        await InitAsync();
        return await _db!.Table<ScanEvent>().Where(e => !e.IsSynced).ToListAsync();
    }

    public async Task<List<Machine>> GetUnsyncedMachinesAsync()
    {
        await InitAsync();
        return await _db!.Table<Machine>().Where(m => !m.IsSynced).ToListAsync();
    }

    public async Task MarkEventSyncedAsync(int id)
    {
        await InitAsync();
        var ev = await _db!.FindAsync<ScanEvent>(id);
        if (ev is not null) { ev.IsSynced = true; await _db.UpdateAsync(ev); }
    }

    public async Task MarkMachineSyncedAsync(int id)
    {
        await InitAsync();
        var m = await _db!.FindAsync<Machine>(id);
        if (m is not null) { m.IsSynced = true; await _db.UpdateAsync(m); }
    }

    // ── Departments ───────────────────────────────────────────────────────────
    public async Task<List<Department>> GetDepartmentsAsync()
    {
        await InitAsync();
        return await _db!.Table<Department>().ToListAsync();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    public static string HashPassword(string password)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(password + "LEONI_SALT_2026");
        return Convert.ToHexString(sha.ComputeHash(bytes));
    }
}
