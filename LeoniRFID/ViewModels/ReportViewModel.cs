using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeoniRFID.Models;
using LeoniRFID.Services;
using System.Collections.ObjectModel;

namespace LeoniRFID.ViewModels;

public partial class ReportViewModel : BaseViewModel
{
    private readonly DatabaseService _db;
    private readonly ExcelService    _excel;

    public ReportViewModel(DatabaseService db, ExcelService excel)
    {
        _db    = db;
        _excel = excel;
        Title  = "Rapports & Export";
        
        // Default filters
        StartDate = DateTime.Today.AddDays(-7);
        EndDate   = DateTime.Today;
    }

    [ObservableProperty] private DateTime _startDate;
    [ObservableProperty] private DateTime _endDate;
    [ObservableProperty] private string?  _selectedDepartment = "Tous";
    [ObservableProperty] private string?  _selectedStatus     = "Tous";

    public List<string> Departments { get; } = ["Tous", "LTN1", "LTN2", "LTN3"];
    public List<string> Statuses    { get; } = ["Tous", "Installed", "Removed", "Maintenance"];

    public ObservableCollection<Machine> FilteredMachines { get; } = [];

    [RelayCommand]
    public async Task RunReportAsync()
    {
        IsBusy = true;
        try
        {
            var all = await _db.GetAllMachinesAsync();
            
            var query = all.AsEnumerable();

            if (SelectedDepartment != "Tous")
                query = query.Where(m => m.Department == SelectedDepartment);
            
            if (SelectedStatus != "Tous")
                query = query.Where(m => m.Status == SelectedStatus);

            // Filter by date range (if applicable to installation/exit)
            // query = query.Where(m => m.InstallationDate >= StartDate && m.InstallationDate <= EndDate);

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
            // Get all events for these machines for a complete export
            var allEvents = await _db.GetRecentEventsAsync(500);
            
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
