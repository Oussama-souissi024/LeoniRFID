using LeoniRFID.ViewModels;

namespace LeoniRFID.Views;

public partial class MachineDetailPage : ContentPage
{
    public MachineDetailPage(MachineDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
