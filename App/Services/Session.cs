using Mass = Microsoft.Maui.Storage;

namespace MediRappel.Services;

/// <summary>
/// Session de l'utilisateur connecté : jeton JWT + profil, persistés dans les
/// préférences de l'appareil lorsque « Se souvenir de moi » est activé.
/// </summary>
public static class Session
{
    const string CleToken = "mr_token";
    const string CleEmail = "mr_email";
    const string CleNom = "mr_nom";
    const string CleId = "mr_id";

    public static string? Token { get; private set; }
    public static int UtilisateurId { get; private set; }
    public static string Nom { get; private set; } = "";
    public static string Email { get; private set; } = "";
    public static bool Connecte => !string.IsNullOrEmpty(Token);

    /// <summary>Mémorise la session après une connexion/inscription réussie.</summary>
    public static void Definir(AuthResponse auth, bool souvenir)
    {
        Token = auth.Token;
        UtilisateurId = auth.Id;
        Nom = auth.Nom;
        Email = auth.Email;
        Medi.Api.DefinirToken(Token);

        if (souvenir)
        {
            Mass.Preferences.Default.Set(CleToken, Token);
            Mass.Preferences.Default.Set(CleEmail, Email);
            Mass.Preferences.Default.Set(CleNom, Nom);
            Mass.Preferences.Default.Set(CleId, UtilisateurId);
        }
        else
        {
            Oublier();
        }
    }

    /// <summary>Restaure la session persistée au démarrage de l'application.</summary>
    public static void Charger()
    {
        var token = Mass.Preferences.Default.Get(CleToken, "");
        if (string.IsNullOrEmpty(token)) return;

        Token = token;
        Email = Mass.Preferences.Default.Get(CleEmail, "");
        Nom = Mass.Preferences.Default.Get(CleNom, "");
        UtilisateurId = Mass.Preferences.Default.Get(CleId, 0);
        Medi.Api.DefinirToken(Token);
    }

    public static void Deconnecter()
    {
        Token = null;
        UtilisateurId = 0;
        Nom = "";
        Email = "";
        Medi.Api.DefinirToken(null);
        Oublier();
    }

    static void Oublier()
    {
        Mass.Preferences.Default.Remove(CleToken);
        Mass.Preferences.Default.Remove(CleEmail);
        Mass.Preferences.Default.Remove(CleNom);
        Mass.Preferences.Default.Remove(CleId);
    }
}
