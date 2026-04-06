// Ce fichier contient la classe `BaseViewModel`.
// Commentaire pédagogique :
// - `BaseViewModel` fournit des propriétés et méthodes communes pour tous les ViewModels (gestion de l'état, messages d'erreur/succès).
// - Utiliser un ViewModel de base évite la duplication de code et facilite les tests.
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LeoniRFID.ViewModels;

// 🎓 Pédagogie PFE : Le Pattern MVVM
// `BaseViewModel` hérite de `ObservableObject` (fourni par CommunityToolkit.Mvvm).
// Cela lui permet de notifier l'interface graphique (les fichiers XAML) à chaque fois
// qu'une variable change dans le code, pour que l'écran se mette à jour tout seul.
public partial class BaseViewModel : ObservableObject
{
    // 🎓 L'attribut [ObservableProperty] est magique : il transforme cette simple 
    // variable privée `_isBusy` en une vraie propriété publique `IsBusy` avec
    // tout le code nécessaire pour rafraichir l'écran quand sa valeur change.
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _successMessage = string.Empty;

    public bool IsNotBusy => !IsBusy;

    protected void SetError(string message)
    {
        ErrorMessage = message;
        SuccessMessage = string.Empty;
    }

    protected void SetSuccess(string message)
    {
        SuccessMessage = message;
        ErrorMessage = string.Empty;
    }

    protected void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }
}
