using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeoniRFID.Models;
using LeoniRFID.Services;
using System.Collections.ObjectModel;

namespace LeoniRFID.ViewModels;

// 🎓 Pédagogie PFE : Module de Reporting (Rapports & Export)
// Ce ViewModel enrichi gère 2 onglets de rapport :
// 1. Rapport Machines : filtre par département, statut, date
// 2. Rapport Maintenance : historique des interventions avec durées
public partial class ReportViewModel : BaseViewModel
{
    private readonly SupabaseService _supabase;
    private readonly ExcelService    _excel;

    public ReportViewModel(SupabaseService supabase, ExcelService excel)
    {
        _supabase = supabase;
        _excel = excel;
        Title  = "Rapports & Export";
        
        StartDate = DateTime.Today.AddDays(-30);
        EndDate   = DateTime.Today;
    }

    // ── Filtres ──────────────────────────────────────────────────────────
    [ObservableProperty] private DateTime _startDate;
    [ObservableProperty] private DateTime _endDate;
    [ObservableProperty] private string?  _selectedDepartment = "Tous";
    [ObservableProperty] private string?  _selectedStatus     = "Tous";

    public List<string> Departments { get; } = ["Tous", "LTN1", "LTN2", "LTN3"];
    public List<string> Statuses    { get; } = ["Tous", "Running", "Broken", "InMaintenance", "Removed"];

    // ── Résultats Machines ───────────────────────────────────────────────
    public ObservableCollection<Machine> FilteredMachines { get; } = [];

    // ── Résultats Maintenance ───────────────────────────────────────────
    public ObservableCollection<MaintenanceSession> MaintenanceSessions { get; } = [];

    // ── Statistiques Maintenance ────────────────────────────────────────
    [ObservableProperty] private int    _totalInterventions;
    [ObservableProperty] private string _averageDuration = "—";
    [ObservableProperty] private string _totalDuration   = "—";

    // ══════════════════════════════════════════════════════════════════════
    //  RAPPORT MACHINES
    // ══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    public async Task RunReportAsync()
    {
        IsBusy = true;
        try
        {
            var allMachines = string.IsNullOrEmpty(SelectedDepartment) || SelectedDepartment == "Tous"
                ? await _supabase.GetAllMachinesAsync()
                : await _supabase.GetMachinesByDepartmentAsync(SelectedDepartment);
            
            var query = allMachines.AsEnumerable();
            
            if (SelectedStatus != "Tous")
                query = query.Where(m => m.Status == SelectedStatus);

            FilteredMachines.Clear();
            foreach (var m in query.OrderBy(x => x.Name))
                FilteredMachines.Add(m);
            
            if (FilteredMachines.Count == 0)
                SetError("Aucun résultat pour ces filtres.");
            else
                ClearMessages();
        }
        finally { IsBusy = false; }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  RAPPORT MAINTENANCE (Historique des interventions avec durées)
    // ══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    public async Task RunMaintenanceReportAsync()
    {
        IsBusy = true;
        try
        {
            // 1. Charger toutes les sessions de maintenance
            var sessions = await _supabase.GetMaintenanceHistoryAsync();

            // 2. Filtrer par date
            var filtered = sessions.Where(s => 
                s.StartedAt.Date >= StartDate.Date && 
                s.StartedAt.Date <= EndDate.Date).ToList();

            // 3. Si un département est sélectionné, filtrer les machines
            if (SelectedDepartment != "Tous" && !string.IsNullOrEmpty(SelectedDepartment))
            {
                var deptMachines = await _supabase.GetMachinesByDepartmentAsync(SelectedDepartment);
                var deptMachineIds = deptMachines.Select(m => m.Id).ToHashSet();
                filtered = filtered.Where(s => deptMachineIds.Contains(s.MachineId)).ToList();
            }

            // 4. Remplir la collection
            MaintenanceSessions.Clear();
            foreach (var s in filtered)
                MaintenanceSessions.Add(s);

            // 5. Calculer les statistiques
            var completed = filtered.Where(s => s.DurationMinutes.HasValue).ToList();
            TotalInterventions = filtered.Count;

            if (completed.Count > 0)
            {
                var avgMin = completed.Average(s => s.DurationMinutes!.Value);
                var totalMin = completed.Sum(s => s.DurationMinutes!.Value);
                AverageDuration = TimeSpan.FromMinutes(avgMin).ToString(@"hh\hmm\m\i\n");
                TotalDuration = TimeSpan.FromMinutes(totalMin).ToString(@"hh\hmm\m\i\n");
            }
            else
            {
                AverageDuration = "—";
                TotalDuration = "—";
            }

            if (MaintenanceSessions.Count == 0)
                SetError("Aucune intervention de maintenance pour cette période.");
            else
                ClearMessages();
        }
        finally { IsBusy = false; }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  EXPORT EXCEL
    // ══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private async Task ExportToExcelAsync()
    {
        if (FilteredMachines.Count == 0)
        {
            await RunReportAsync();
            if (FilteredMachines.Count == 0) return;
        }

        IsBusy = true;
        try
        {
            var allEvents = await _supabase.GetRecentEventsAsync(500);
            
            var stream = _excel.ExportReport(FilteredMachines.ToList(), allEvents);
            
            string fileName = $"Rapport_LEONI_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            
            using (var fileStream = File.Create(filePath))
            {
                await stream.CopyToAsync(fileStream);
            }

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Export Rapport LEONI",
                File  = new ShareFile(filePath)
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Erreur Export", ex.Message, "OK");
        }
        finally { IsBusy = false; }
    }
}
