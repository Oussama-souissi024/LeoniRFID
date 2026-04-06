using System.Globalization;

namespace LeoniRFID.Helpers;

// ── Bool → Invert ─────────────────────────────────────────────────────────────
// 🎓 Pédagogie PFE : Qu'est-ce qu'un Converter ?
// En XAML, un "Binding" transfère une donnée du ViewModel vers l'interface.
// Un Converter modifie cette donnée à la volée avant qu'elle ne soit affichée.
// Ici `InverseBoolConverter` reçoit un booléen (ex: "true") et le transforme en son inverse ("false").
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;
}

// ── Bool → Visibility ─────────────────────────────────────────────────────────
// 🎓 Pédagogie PFE : Afficher/Masquer intelligemment
// Reçoit un booléen et le retourne tel quel. 
// En réalité, MAUI gère la conversion implicite vers l'enum `Visibility` si c'est lié à "IsVisible" en XAML.
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value;
}

// ── Status → Color ────────────────────────────────────────────────────────────
// 🎓 Pédagogie PFE : Converter de couleur (Design)
// Prend le statut (qui est du texte, ex: "Installed") et retourne une couleur .NET MAUI.
// Cela permet de colorer automatiquement le texte en XAML sans écrire de logique.
public class StatusToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "Installed"   => Color.FromArgb("#2ECC71"),   // green
            "Removed"     => Color.FromArgb("#E74C3C"),   // red
            "Maintenance" => Color.FromArgb("#F39C12"),   // orange
            _             => Color.FromArgb("#95A5A6")    // grey
        };
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// ── Status → Badge Background ─────────────────────────────────────────────────
// 🎓 Pédagogie PFE : Fond de badge proportionnel
// Similaire au Converter précédent, mais pour fournir une couleur de fond sombre adaptée.
public class StatusToBadgeColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "Installed"   => Color.FromArgb("#1A4731"),
            "Removed"     => Color.FromArgb("#4A1A1A"),
            "Maintenance" => Color.FromArgb("#4A3A1A"),
            _             => Color.FromArgb("#2D2D2D")
        };
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// ── DateTime → Formatted String ───────────────────────────────────────────────
// 🎓 Pédagogie PFE : Centraliser le formatage
// S'assure que *toutes* les dates de l'application s'affichent au format "Jour/Mois/Année Heure:Minute".
public class DateTimeFormatConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime dt && dt != default)
            return dt.ToString("dd/MM/yyyy HH:mm");
        return "—";
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// ── Null → Visibility ─────────────────────────────────────────────────────────
// 🎓 Pédagogie PFE : Null ou Pas Null
// Idéal pour cacher une section ou un message (ex: les "Notes") si le texte est vide ou null.
public class NullToVisibleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is null;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class NotNullToVisibleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not null;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
