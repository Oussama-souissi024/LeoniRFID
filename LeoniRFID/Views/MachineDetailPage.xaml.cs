using LeoniRFID.ViewModels;

namespace LeoniRFID.Views;

public partial class MachineDetailPage : ContentPage
{
    public MachineDetailPage(MachineDetailViewModel viewModel)
    {
        // Commentaire pédagogique :
        // - Les pages reçoivent leur ViewModel via DI (MauiProgram) ; cela facilite le test unitaire et la séparation des responsabilités.
        InitializeComponent();
        BindingContext = viewModel;
    }
}
