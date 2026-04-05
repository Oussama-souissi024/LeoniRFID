using LeoniRFID.ViewModels;

namespace LeoniRFID.Views;

// 🎓 Pédagogie PFE : Page de Détails (Navigation par Paramètre)
// Contrairement aux autres pages, cette page gère un cas de navigation complexe.
// Elle reçoit un paramètre externe (ex: "machineId=123") via l'URL de route dynamique.
// Le ViewModel `MachineDetailViewModel` va intercepter cet ID pour l'utiliser.
public partial class MachineDetailPage : ContentPage
{
    public MachineDetailPage(MachineDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
