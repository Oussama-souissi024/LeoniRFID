using System.Globalization;

namespace LeoniRFID.Helpers;

// ── Bool → Invert ─────────────────────────────────────────────────────────────
// Commentaire pédagogique :
// - Les converters permettent de transformer des valeurs du ViewModel avant affichage en XAML.
// - Ici `InverseBoolConverter` inverse un booléen (utile pour afficher/masquer des éléments).
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;
}

// ── Bool → Visibility ─────────────────────────────────────────────────────────
// Commentaire pédagogique :
// - `BoolToVisibilityConverter` convertit un booléen en visibilité (true → visible, false → collapsed).
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value;
}

// ── Status → Color ────────────────────────────────────────────────────────────
// Commentaire pédagogique :
// - `StatusToColorConverter` mappe un statut métier sur une couleur d'UI (améliore la lisibilité).
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
// Commentaire pédagogique :
// - `StatusToBadgeColorConverter` fournit une couleur de fond pour les badges de statut.
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
// Commentaire pédagogique :
// - `DateTimeFormatConverter` formate les dates pour affichage (centralise le formatage en un seul endroit).
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
// Commentaire pédagogique :
// - `NullToVisibleConverter` et `NotNullToVisibleConverter` facilitent l'affichage conditionnel basé sur la nullité des données.
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
