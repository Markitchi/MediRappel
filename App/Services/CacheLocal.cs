using System.Text.Json;

namespace MediRappel.Services;

/// <summary>
/// Cache local (snapshot JSON) pour le mode hors ligne : on lit l'API quand le
/// réseau est là, et on retombe sur ce cache sinon.
/// </summary>
public static class CacheLocal
{
    static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    static string Chemin(string cle) =>
        Path.Combine(FileSystem.AppDataDirectory, $"cache_{cle}.json");

    public static void Sauver<T>(string cle, T valeur)
    {
        try { File.WriteAllText(Chemin(cle), JsonSerializer.Serialize(valeur, Options)); }
        catch (IOException) { /* disque indisponible : le cache est facultatif */ }
        catch (UnauthorizedAccessException) { }
    }

    public static T? Lire<T>(string cle) where T : class
    {
        try
        {
            var chemin = Chemin(cle);
            return File.Exists(chemin)
                ? JsonSerializer.Deserialize<T>(File.ReadAllText(chemin), Options)
                : null;
        }
        catch (IOException) { return null; }
        catch (JsonException) { return null; }
    }

    /// <summary>Taille cumulée des fichiers de cache (octets).</summary>
    public static long Taille()
    {
        try
        {
            return Directory.EnumerateFiles(FileSystem.AppDataDirectory, "cache_*.json")
                .Sum(f => new FileInfo(f).Length);
        }
        catch (IOException) { return 0; }
    }

    public static void Vider()
    {
        try
        {
            foreach (var f in Directory.EnumerateFiles(FileSystem.AppDataDirectory, "cache_*.json"))
                File.Delete(f);
        }
        catch (IOException) { }
    }

    /// <summary>Supprime une entrée du cache (invalidation ciblée).</summary>
    public static void Supprimer(string cle)
    {
        try
        {
            var chemin = Chemin(cle);
            if (File.Exists(chemin)) File.Delete(chemin);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
