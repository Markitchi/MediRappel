using System.Globalization;
using MediRappel.Models;

namespace MediRappel.Converters;

/// <summary>
/// La couleur est TOUJOURS doublée d'une icône et d'un libellé :
/// aucune information n'est portée par la seule couleur (charte).
/// </summary>
public sealed class EtatVersCouleur : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool fond = parameter?.ToString() == "fond";
        if (value is not EtatDose e) return Colors.Transparent;

        return e switch
        {
            EtatDose.Faite => Couleur(fond, "OkContainer", "OnOkContainer"),
            EtatDose.Manquee => Couleur(fond, "MissedContainer", "OnMissedContainer"),
            EtatDose.Partielle => Couleur(fond, "DueContainer", "OnDueContainer"),
            _ => fond ? Couleur(true, "SurfaceHigh", "SurfaceHigh") : Couleur(false, "OnSurfaceVariant", "OnSurfaceVariant"),
        };
    }

    static Color Couleur(bool fond, string clair, string fonce) =>
        (Color)Application.Current!.Resources[fond ? clair : fonce];

    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) =>
        throw new NotSupportedException();
}

/// <summary>Icône Material associée à l'état d'une dose.</summary>
public sealed class EtatVersIcone : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            EtatDose.Faite => "check_circle",
            EtatDose.Manquee => "error",
            EtatDose.Partielle => "schedule",
            _ => "radio_button_unchecked",
        };

    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) =>
        throw new NotSupportedException();
}

/// <summary>Badge de statut d'ordonnance : teinte de fond.</summary>
public sealed class StatutVersFond : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c) =>
        value as string == "Active"
            ? (Color)Application.Current!.Resources["OkContainer"]
            : (Color)Application.Current!.Resources["SurfaceHigh"];

    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) =>
        throw new NotSupportedException();
}

/// <summary>Badge de statut : couleur du texte (toujours doublée du libellé).</summary>
public sealed class StatutVersTexte : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c) =>
        value as string == "Active"
            ? (Color)Application.Current!.Resources["OnOkContainer"]
            : (Color)Application.Current!.Resources["OnSurfaceVariant"];

    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) =>
        throw new NotSupportedException();
}

public sealed class BooleenVersInvert : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c) =>
        !(value is bool b && b);

    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) =>
        !(v is bool b && b);
}

/// <summary>Chip sélectionnée : fond plein ; sinon transparent.</summary>
public sealed class SelectionVersFond : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c) =>
        value is true
            ? (Color)Application.Current!.Resources["Primary"]
            : (Color)Application.Current!.Resources["SurfaceContainer"];

    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) =>
        throw new NotSupportedException();
}

/// <summary>Chip sélectionnée : texte contrasté sur fond plein.</summary>
public sealed class SelectionVersTexte : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c) =>
        value is true
            ? (Color)Application.Current!.Resources["OnPrimary"]
            : (Color)Application.Current!.Resources["OnSurface"];

    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) =>
        throw new NotSupportedException();
}

/// <summary>Booléen → visibilité (true = visible).</summary>
public sealed class VersVisible : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c) =>
        value is bool b && b;

    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) =>
        throw new NotSupportedException();
}

/// <summary>Tendance hebdomadaire → couleur de la barre de l'histogramme.</summary>
public sealed class TendanceVersCouleur : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c) => value switch
    {
        TendanceSemaine.Manquee => (Color)Application.Current!.Resources["Error"],
        TendanceSemaine.Partielle => (Color)Application.Current!.Resources["BarrePartielle"],
        _ => (Color)Application.Current!.Resources["Primary"],
    };

    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) =>
        throw new NotSupportedException();
}

/// <summary>Octets → « 24,3 Ko ».</summary>
public sealed class VersTaille : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c)
    {
        var o = value is long l ? l : System.Convert.ToInt64(value ?? 0L);
        if (o < 1024) return $"{o} o";
        if (o < 1024 * 1024) return $"{o / 1024.0:0.#} Ko";
        return $"{o / (1024.0 * 1024.0):0.#} Mo";
    }

    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) =>
        throw new NotSupportedException();
}
