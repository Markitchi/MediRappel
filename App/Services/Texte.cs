namespace MediRappel.Services;

/// <summary>Petites fonctions de mise en forme du texte (français).</summary>
public static class Texte
{
    public static string Capitaliser(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s[1..];

    public static string Pluriel(int n, string singulier, string? pluriel = null) =>
        n > 1 ? $"{n} {pluriel ?? singulier + "s"}" : $"{n} {singulier}";

    public static string Heure(DateTime d) => d.ToString("HH:mm");
}
