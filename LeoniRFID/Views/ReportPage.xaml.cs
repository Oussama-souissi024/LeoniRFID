using LeoniRFID.ViewModels;

namespace LeoniRFID.Views;

// 🎓 Pédagogie PFE : Architecture Orientée Composant
// Vous remarquerez que le code-behind est quasiment vide (3 lignes).
// C'est un indicateur d'une architecture UI saine. Toute l'implémentation
// (création PDF, filtrage de listes) est découplée dans le ViewModel et les Services.
public partial class ReportPage : ContentPage
{
    public ReportPage(ReportViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
