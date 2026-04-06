using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeoniRFID.Models;
using LeoniRFID.Services;
using System.Collections.ObjectModel;

namespace LeoniRFID.ViewModels;

// 🎓 Pédagogie PFE : Module d'Administration
// Ce ViewModel gère les opérations réservées aux administrateurs :
// - Import de fichiers Excel contenant la liste des machines
// - Génération de fichiers Excel de test (template)
// - Suppression de machines
// - Navigation vers les détails d'une machine
// C'est un bon exemple de séparation des responsabilités : le ViewModel orchestre,
// mais délègue le travail réel aux Services (SupabaseService, ExcelService).
public partial class AdminViewModel : BaseViewModel
{
    private readonly SupabaseService _supabase;
    private readonly ExcelService    _excel;

    public AdminViewModel(SupabaseService supabase, ExcelService excel)
    {
        _supabase = supabase;
        _excel = excel;
        Title  = "Administration";
    }

    // Initialiser les collections pour éviter les NullReferenceException côté UI
    public ObservableCollection<Machine> Machines { get; } = new ObservableCollection<Machine>();
    public ObservableCollection<Profile>  Users    { get; } = new ObservableCollection<Profile>();

    [ObservableProperty] private string _importStatus = string.Empty;

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var machines = await _supabase.GetAllMachinesAsync();
            Machines.Clear();
            foreach (var m in machines) Machines.Add(m);

            var usersList = await _supabase.GetAllProfilesAsync();
            Users.Clear();
            foreach (var u in usersList) Users.Add(u);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ImportExcelAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Sélectionner le fichier Excel LEONI",
                FileTypes   = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.Android, new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" } }
                })
            });

            if (result == null) return;

            IsBusy = true;
            ImportStatus = "Lecture du fichier...";

            using var stream = await result.OpenReadAsync();
            var imported = _excel.ImportMachines(stream);

            if (imported.Count > 0)
            {
                await _supabase.BulkInsertMachinesAsync(imported);
                await LoadAsync();
                await Shell.Current.DisplayAlert("Succès", $"{imported.Count} machines importées avec succès.", "OK");
            }
            else
            {
                await Shell.Current.DisplayAlert("Erreur", "Aucune donnée valide trouvée dans le fichier.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Erreur", $"Erreur d'import : {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
            ImportStatus = string.Empty;
        }
    }

    [RelayCommand]
    private async Task EditMachineAsync(Machine machine)
    {
        await Shell.Current.GoToAsync($"machinedetail?machineId={machine.Id}");
    }

    [RelayCommand]
    private async Task GenerateSampleExcelAsync()
    {
        try
        {
            IsBusy = true;
            var stream = _excel.GenerateTestExcel();
            
            // On Android, we'd typically save this to the Downloads folder
            string targetPath = Path.Combine(FileSystem.CacheDirectory, "Machines_LEONI_Template.xlsx");
            using (var fileStream = File.Create(targetPath))
            {
                await stream.CopyToAsync(fileStream);
            }

            await Shell.Current.DisplayAlert("Template Généré", $"Fichier disponible : {targetPath}", "OK");
            
            // Share the file
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Template Excel LEONI",
                File  = new ShareFile(targetPath)
            });
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task DeleteMachineAsync(Machine machine)
    {
        bool confirm = await Shell.Current.DisplayAlert("Confirmer", $"Supprimer {machine.Name} ?", "Oui", "Non");
        if (!confirm) return;

        await _supabase.DeleteMachineAsync(machine);
        Machines.Remove(machine);
    }
}
