namespace LeoniRFID.Services;

public class SyncService
{
    private readonly DatabaseService _db;
    private readonly ApiService _api;
    private bool _isSyncing;

    public SyncService(DatabaseService db, ApiService api)
    {
        _db  = db;
        _api = api;
    }

    public event EventHandler<string>? SyncStatusChanged;

    /// <summary>
    /// Full sync: push local unsynced data → pull fresh server data.
    /// Safe to call anytime; exits early if offline or already syncing.
    /// </summary>
    public async Task<bool> SyncAsync()
    {
        if (_isSyncing) return false;

        var connectivity = Connectivity.Current.NetworkAccess;
        if (connectivity is not NetworkAccess.Internet)
        {
            SyncStatusChanged?.Invoke(this, "Hors ligne — synchronisation différée.");
            return false;
        }

        _isSyncing = true;
        SyncStatusChanged?.Invoke(this, "Synchronisation en cours…");

        try
        {
            bool serverReachable = await _api.IsServerReachableAsync();
            if (!serverReachable)
            {
                SyncStatusChanged?.Invoke(this, "Serveur inaccessible.");
                return false;
            }

            // ── Push unsynced machines ──────────────────────────────────────
            var unsyncedMachines = await _db.GetUnsyncedMachinesAsync();
            foreach (var m in unsyncedMachines)
            {
                if (await _api.UpdateMachineAsync(m))
                    await _db.MarkMachineSyncedAsync(m.Id);
            }

            // ── Push unsynced events ────────────────────────────────────────
            var unsyncedEvents = await _db.GetUnsyncedEventsAsync();
            foreach (var e in unsyncedEvents)
            {
                if (await _api.PostScanEventAsync(e))
                    await _db.MarkEventSyncedAsync(e.Id);
            }

            // ── Pull fresh machine list ─────────────────────────────────────
            var serverMachines = await _api.GetMachinesAsync();
            if (serverMachines is not null)
            {
                foreach (var m in serverMachines)
                {
                    m.IsSynced = true;
                    await _db.SaveMachineAsync(m);
                }
            }

            SyncStatusChanged?.Invoke(this,
                $"Synchronisé — {unsyncedMachines.Count} machines, {unsyncedEvents.Count} événements.");
            return true;
        }
        catch (Exception ex)
        {
            SyncStatusChanged?.Invoke(this, $"Erreur sync : {ex.Message}");
            return false;
        }
        finally
        {
            _isSyncing = false;
        }
    }

    /// <summary>
    /// Start background periodic sync every 5 minutes.
    /// </summary>
    public void StartPeriodicSync(int intervalMinutes = 5)
    {
        var timer = new System.Timers.Timer(TimeSpan.FromMinutes(intervalMinutes).TotalMilliseconds);
        timer.Elapsed += async (s, e) => await SyncAsync();
        timer.Start();
    }
}
